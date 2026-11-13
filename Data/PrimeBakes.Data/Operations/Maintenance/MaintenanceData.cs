using Dapper;

using Microsoft.Data.SqlClient;

using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Maintenance;
using PrimeBakes.Models.Operations.Settings;

using System.Data;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class MaintenanceData
{
	public static async Task<int?> CheckBackupPending()
	{
		int _defaultBackupReminderDays = 7;

		try
		{
			var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.BackupReminderDays);
			var reminderDays = int.TryParse(setting?.Value, out var days) ? days : _defaultBackupReminderDays;
			var lastBackup = await SyncData.LoadLastBackupDate();
			var elapsed = lastBackup is null ? int.MaxValue : (int)(DateTime.Now - lastBackup.Value).TotalDays;

			if (elapsed >= reminderDays)
				return elapsed;
		}
		catch { }

		return null;
	}

	public static async Task RebuildIndexes()
	{
		using SqlConnection connection = new(SqlDataAccess.DatabaseConnection);
		await connection.ExecuteAsync(OperationNames.RebuildIndexes, commandType: CommandType.StoredProcedure, commandTimeout: 0);
	}

	public static async Task<DatabaseSizeModel> LoadDatabaseSize() =>
		(await SqlDataAccess.LoadData<DatabaseSizeModel, dynamic>(OperationNames.LoadDatabaseSize, new { })).FirstOrDefault();
}
