using PrimeBakes.Data.Common;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Operations.Maintenance;

internal static class SyncNotify
{
	internal static async Task Notify(SyncData.SyncResult result, DateTime? previousBackup, DateTime backupDateTime, int userId)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1)];

		var user = users.FirstOrDefault(_ => _.Id == userId);
		var userName = user?.Name ?? "Unknown User";
		var transactionNo = backupDateTime.ToString("yyyyMMddHHmm");

		await BackupNotification(users, result, backupDateTime, transactionNo, userName);
		await BackupMail(result, previousBackup, backupDateTime, transactionNo, userName);
	}

	private static async Task BackupNotification(List<UserModel> users, SyncData.SyncResult result,
		DateTime backupDateTime, string transactionNo, string userName)
	{
		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Database Backup",
			TransactionNo = transactionNo,
			Action = NotifyType.Created,
			LocationName = "Main Location",
			Details = new Dictionary<string, string>
			{
				["🗄️ Tables"] = $"{result.Tables} synced, {result.Seeded} fully copied, {result.Skipped} unchanged",
				["🔢 Rows"] = $"{result.Copied:N0} copied, {result.Removed:N0} removed",
				["⏱️ Duration"] = $"{result.Elapsed.TotalSeconds:N1}s",
				["📅 Date"] = backupDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["👤 By"] = userName
			},
			Remarks = null
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task BackupMail(SyncData.SyncResult result, DateTime? previousBackup,
		DateTime backupDateTime, string transactionNo, string userName)
	{
		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Database Backup",
			TransactionNo = transactionNo,
			Action = NotifyType.Created,
			LocationName = "Main Location",
			Details = new Dictionary<string, string>
			{
				["Backup Date"] = backupDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Previous Backup"] = previousBackup?.ToString("dd MMM yyyy, hh:mm tt") ?? "Never",
				["Tables Synced"] = result.Tables.ToString(),
				["Tables Fully Copied"] = result.Seeded.ToString(),
				["Tables Unchanged"] = result.Skipped.ToString(),
				["Rows Copied"] = result.Copied.ToString("N0"),
				["Rows Removed"] = result.Removed.ToString("N0"),
				["Duration"] = $"{result.Elapsed.TotalSeconds:N1}s",
				["Backed Up By"] = userName
			},
			Remarks = null
		};

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
