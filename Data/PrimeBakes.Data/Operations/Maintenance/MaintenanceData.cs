using Dapper;

using Microsoft.Data.SqlClient;

using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Maintenance;
using PrimeBakes.Models.Operations.Settings;

using System.Data;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class MaintenanceData
{
	public static async Task<DatabaseSizeModel> LoadDatabaseSize() =>
		(await SqlDataAccess.LoadData<DatabaseSizeModel, dynamic>(OperationNames.LoadDatabaseSize, new { })).FirstOrDefault();

	public static async Task<int?> CheckBackupPending()
	{
		try
		{
			var reminderDays = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.BackupReminderDays)).Value);
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

	public static async Task DeleteExpiredRoutineAuditTrailRecords()
	{
		var retentionDays = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.RoutineAuditTrailRetentionDays)).Value);
		var cutoff = DateTime.Now.AddDays(-retentionDays);

		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.Insert.ToString(), cutoff.AddYears(-10), cutoff, false);
		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.Report.ToString(), cutoff.AddYears(-10), cutoff, false);
		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.Notification.ToString(), cutoff.AddYears(-10), cutoff, false);
		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.Login.ToString(), cutoff.AddYears(-10), cutoff, false);
		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.Logout.ToString(), cutoff.AddYears(-10), cutoff, false);
		await AuditTrailData.DeleteAuditTrailByActionDate(AuditTrailActionTypes.PageVisit.ToString(), cutoff.AddYears(-10), cutoff, false);
	}
}
