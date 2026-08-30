using Dapper;

using Microsoft.Data.SqlClient;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;

using System.Data;
using System.Diagnostics;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class BackupData
{
	private const int _batchSize = 2000;
	private const char _keySeparator = (char)31;

	private sealed record TableInfo(string TableName, string KeyColumn);
	private sealed record HashRow(string KeyValue, byte[] Hash);

	public static async Task<string> Backup()
	{
		if (CommonSecrets.DatabaseConnection != ConnectionType.Azure)
			throw new InvalidOperationException("Backup can only be run against the production server.");

		if (Secrets.AzureConnectionString == Secrets.AzureTestingConnectionString)
			throw new InvalidOperationException("The source and backup servers cannot be the same.");

		using SqlConnection source = new(Secrets.AzureConnectionString);
		using SqlConnection backup = new(Secrets.AzureTestingConnectionString);

		await source.OpenAsync();
		await backup.OpenAsync();

		var sourceTables = await LoadTableNames(source);
		var backupTableNames = (await LoadTableNames(backup)).Select(table => table.TableName).ToHashSet();
		var tables = sourceTables.Where(table => backupTableNames.Contains(table.TableName)).ToList();

		int copied = 0;
		int removed = 0;
		var stopwatch = Stopwatch.StartNew();

		await ToggleForeignKeys(backup, false);

		try
		{
			foreach (var table in tables)
			{
				var (tableCopied, tableRemoved) = await BackupTable(source, backup, table);
				copied += tableCopied;
				removed += tableRemoved;
			}
		}
		finally
		{
			await ToggleForeignKeys(backup, true);
		}

		stopwatch.Stop();

		int skipped = sourceTables.Count - tables.Count;

		return $"{tables.Count} tables in {stopwatch.Elapsed.TotalSeconds:N1}s. {copied:N0} rows copied, {removed:N0} removed."
			+ (skipped > 0 ? $" {skipped} not on the backup server, skipped." : string.Empty);
	}

	private static async Task<List<TableInfo>> LoadTableNames(SqlConnection connection) =>
		(await connection.QueryAsync<TableInfo>(OperationNames.LoadTableNames, commandType: CommandType.StoredProcedure)).ToList();

	private static async Task ToggleForeignKeys(SqlConnection connection, bool enable) =>
		await connection.ExecuteAsync(OperationNames.ToggleForeignKeys, new { Enable = enable },
			commandType: CommandType.StoredProcedure, commandTimeout: 0);

	private static async Task<(int Copied, int Removed)> BackupTable(SqlConnection source, SqlConnection backup, TableInfo table)
	{
		if (string.IsNullOrWhiteSpace(table.KeyColumn))
		{
			await backup.ExecuteAsync(OperationNames.DeleteTableData, new { table.TableName },
				commandType: CommandType.StoredProcedure, commandTimeout: 0);

			return (await CopyRows(source, backup, table, null), 0);
		}

		var sourceHashes = await LoadHashes(source, table);
		var backupHashes = await LoadHashes(backup, table);

		List<string> toDelete = [];
		List<string> toCopy = [];

		foreach (var (key, hash) in sourceHashes)
			if (!backupHashes.TryGetValue(key, out var backupHash))
				toCopy.Add(key);
			else if (!backupHash.SequenceEqual(hash))
			{
				toDelete.Add(key);
				toCopy.Add(key);
			}

		foreach (var key in backupHashes.Keys)
			if (!sourceHashes.ContainsKey(key))
				toDelete.Add(key);

		foreach (var batch in toDelete.Chunk(_batchSize))
			await backup.ExecuteAsync(OperationNames.DeleteTableDataByKeys,
				new { table.TableName, table.KeyColumn, Keys = string.Join(_keySeparator, batch) },
				commandType: CommandType.StoredProcedure, commandTimeout: 0);

		int copied = 0;

		foreach (var batch in toCopy.Chunk(_batchSize))
			copied += await CopyRows(source, backup, table, batch);

		return (copied, toDelete.Count);
	}

	private static async Task<Dictionary<string, byte[]>> LoadHashes(SqlConnection connection, TableInfo table)
	{
		var rows = await connection.QueryAsync<HashRow>(OperationNames.LoadTableHashes,
			new { table.TableName, table.KeyColumn }, commandType: CommandType.StoredProcedure, commandTimeout: 0);

		return rows.ToDictionary(row => row.KeyValue, row => row.Hash);
	}

	private static async Task<int> CopyRows(SqlConnection source, SqlConnection backup, TableInfo table, string[] keys)
	{
		using SqlCommand command = new(keys is null ? CommonNames.LoadTableData : OperationNames.LoadTableDataByKeys, source)
		{
			CommandType = CommandType.StoredProcedure,
			CommandTimeout = 0
		};

		command.Parameters.AddWithValue("@TableName", table.TableName);

		if (keys is not null)
		{
			command.Parameters.AddWithValue("@KeyColumn", table.KeyColumn);
			command.Parameters.AddWithValue("@Keys", string.Join(_keySeparator, keys));
		}

		using var reader = await command.ExecuteReaderAsync();

		using SqlBulkCopy bulkCopy = new(backup, SqlBulkCopyOptions.KeepIdentity, null)
		{
			DestinationTableName = $"[dbo].[{table.TableName}]",
			BulkCopyTimeout = 0
		};

		for (int i = 0; i < reader.FieldCount; i++)
			bulkCopy.ColumnMappings.Add(reader.GetName(i), reader.GetName(i));

		await bulkCopy.WriteToServerAsync(reader);

		return bulkCopy.RowsCopied;
	}
}
