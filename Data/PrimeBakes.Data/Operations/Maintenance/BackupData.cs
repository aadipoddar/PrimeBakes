using Dapper;

using Microsoft.Data.SqlClient;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Maintenance;

using System.Data;
using System.Diagnostics;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class BackupData
{
	private const int _batchSize = 2000;
	private const char _keySeparator = (char)31;
	private const int _connectRetryCount = 10;
	private const int _connectRetryInterval = 10;
	private const int _connectTimeout = 90;
	private const int _bulkCopyBatchSize = 10000;
	private const string _backupMarker = "LastBackup";

	private sealed record TableInfo(string TableName, string KeyColumn);
	private sealed record VersionInfo(string TableName, long CurrentVersion, long MinValidVersion);
	private sealed record ChangeRow(string KeyValue, string Operation);

	public static async Task<string> Backup() =>
		await RunSync(Secrets.AzureTestingConnectionString, _backupMarker);

	public static async Task<string> SyncToLocalClient() =>
		await RunSync(Secrets.LocalClientConnectionString, null);

	private static async Task<string> RunSync(string backupConnectionString, string markerTable)
	{
		using SqlConnection source = new(WithConnectionResiliency(Secrets.AzureConnectionString));
		using SqlConnection backup = new(WithConnectionResiliency(backupConnectionString));

		await source.OpenAsync();
		await backup.OpenAsync();

		var tables = await LoadSyncableTables(source, backup);
		var versions = await LoadVersions(source);
		var syncedVersions = await LoadSyncedVersions(backup);

		int copied = 0;
		int removed = 0;
		int seeded = 0;
		int skipped = 0;
		var stopwatch = Stopwatch.StartNew();

		await ToggleForeignKeys(backup, false);

		try
		{
			foreach (var table in tables)
			{
				if (!versions.TryGetValue(table.TableName, out var version))
					continue;

				bool synced = syncedVersions.TryGetValue(table.TableName, out var lastVersion);

				if (synced && lastVersion == version.CurrentVersion)
				{
					skipped++;
					continue;
				}

				if (!synced || lastVersion < version.MinValidVersion)
				{
					copied += await SeedTable(source, backup, table);
					seeded++;
				}
				else
				{
					var (tableCopied, tableRemoved) = await SyncTable(source, backup, table, lastVersion);
					copied += tableCopied;
					removed += tableRemoved;
				}

				await SaveVersion(backup, table.TableName, version.CurrentVersion);
			}
		}
		finally
		{
			await ToggleForeignKeys(backup, true);
		}

		if (markerTable is not null)
			await SaveVersion(source, markerTable, versions.Values.Max(version => version.CurrentVersion));

		stopwatch.Stop();

		return $"{tables.Count} tables in {stopwatch.Elapsed.TotalSeconds:N1}s. {copied:N0} rows copied, {removed:N0} removed."
			+ (seeded > 0 ? $" {seeded} fully copied." : string.Empty)
			+ (skipped > 0 ? $" {skipped} unchanged." : string.Empty);
	}

	public static async Task<DateTime?> LoadLastBackupDate()
	{
		using SqlConnection source = new(WithConnectionResiliency(Secrets.AzureConnectionString));
		await source.OpenAsync();

		return (await source.QueryAsync<SyncVersionModel>(CommonNames.LoadTableData,
			new { TableName = OperationNames.SyncVersion }, commandType: CommandType.StoredProcedure))
			.FirstOrDefault(sync => sync.TableName == _backupMarker)?.LastSyncedAt;
	}

	private static string WithConnectionResiliency(string connectionString) =>
		new SqlConnectionStringBuilder(connectionString)
		{
			ConnectRetryCount = _connectRetryCount,
			ConnectRetryInterval = _connectRetryInterval,
			ConnectTimeout = _connectTimeout
		}.ConnectionString;

	private static async Task<List<TableInfo>> LoadSyncableTables(SqlConnection source, SqlConnection backup)
	{
		var sourceTables = await LoadTableNames(source);
		var backupTableNames = (await LoadTableNames(backup)).Select(table => table.TableName).ToHashSet();

		return [.. sourceTables.Where(table => table.TableName != OperationNames.SyncVersion && backupTableNames.Contains(table.TableName))];
	}

	private static async Task<List<TableInfo>> LoadTableNames(SqlConnection connection) =>
		[.. (await connection.QueryAsync<TableInfo>(OperationNames.LoadTableNames, commandType: CommandType.StoredProcedure))];

	private static async Task<Dictionary<string, VersionInfo>> LoadVersions(SqlConnection source) =>
		(await source.QueryAsync<VersionInfo>(OperationNames.LoadTableChangeVersions, commandType: CommandType.StoredProcedure))
			.ToDictionary(version => version.TableName);

	private static async Task<Dictionary<string, long>> LoadSyncedVersions(SqlConnection backup) =>
		(await backup.QueryAsync<SyncVersionModel>(CommonNames.LoadTableData,
			new { TableName = OperationNames.SyncVersion }, commandType: CommandType.StoredProcedure))
			.ToDictionary(sync => sync.TableName, sync => sync.Version);

	private static async Task SaveVersion(SqlConnection backup, string tableName, long version) =>
		await backup.ExecuteAsync(OperationNames.InsertSyncVersion, new { TableName = tableName, Version = version },
			commandType: CommandType.StoredProcedure, commandTimeout: 0);

	private static async Task ToggleForeignKeys(SqlConnection connection, bool enable) =>
		await connection.ExecuteAsync(OperationNames.ToggleForeignKeys, new { Enable = enable },
			commandType: CommandType.StoredProcedure, commandTimeout: 0);

	private static async Task<int> SeedTable(SqlConnection source, SqlConnection backup, TableInfo table)
	{
		await backup.ExecuteAsync(OperationNames.DeleteTableData, new { table.TableName },
			commandType: CommandType.StoredProcedure, commandTimeout: 0);

		return await CopyRows(source, backup, table, null);
	}

	private static async Task<(int Copied, int Removed)> SyncTable(SqlConnection source, SqlConnection backup, TableInfo table, long lastVersion)
	{
		var changes = (await source.QueryAsync<ChangeRow>(OperationNames.LoadTableChanges,
			new { table.TableName, table.KeyColumn, LastVersion = lastVersion },
			commandType: CommandType.StoredProcedure, commandTimeout: 0)).ToList();

		if (changes.Count == 0)
			return (0, 0);

		foreach (var batch in changes.Select(change => change.KeyValue).Chunk(_batchSize))
			await backup.ExecuteAsync(OperationNames.DeleteTableDataByKeys,
				new { table.TableName, table.KeyColumn, Keys = string.Join(_keySeparator, batch) },
				commandType: CommandType.StoredProcedure, commandTimeout: 0);

		var toCopy = changes.Where(change => change.Operation != "D").Select(change => change.KeyValue).ToArray();

		int copied = 0;

		foreach (var batch in toCopy.Chunk(_batchSize))
			copied += await CopyRows(source, backup, table, batch);

		return (copied, changes.Count - toCopy.Length);
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

		using SqlBulkCopy bulkCopy = new(backup, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock, null)
		{
			DestinationTableName = $"[dbo].[{table.TableName}]",
			BulkCopyTimeout = 0,
			BatchSize = _bulkCopyBatchSize,
			EnableStreaming = true
		};

		for (int i = 0; i < reader.FieldCount; i++)
			bulkCopy.ColumnMappings.Add(reader.GetName(i), reader.GetName(i));

		await bulkCopy.WriteToServerAsync(reader);

		return bulkCopy.RowsCopied;
	}
}
