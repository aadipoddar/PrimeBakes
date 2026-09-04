using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Exports.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenIssue;

internal static class KitchenIssueReturnNotify
{
	internal static async Task Notify(int kitchenIssueReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await KitchenIssueReturnNotification(kitchenIssueReturnId, type);
		await KitchenIssueReturnMail(kitchenIssueReturnId, type, previousInvoice);
	}

	private static async Task KitchenIssueReturnNotification(int kitchenIssueReturnId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var kitchenIssueReturn = await CommonData.LoadTableDataById<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, kitchenIssueReturnId);

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Kitchen Issue Return",
			TransactionNo = kitchenIssueReturn.TransactionNo,
			Action = type,
			LocationName = kitchenIssueReturn.KitchenName,
			Details = new Dictionary<string, string>
			{
				["🍳 Kitchen"] = kitchenIssueReturn.KitchenName,
				["📦 Items"] = $"{kitchenIssueReturn.TotalItems} | Qty: {kitchenIssueReturn.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = kitchenIssueReturn.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = kitchenIssueReturn.LastModifiedByUserName ?? kitchenIssueReturn.CreatedByName,
				["📅 Date"] = kitchenIssueReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = kitchenIssueReturn.Remarks
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task KitchenIssueReturnMail(int kitchenIssueReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var kitchenIssueReturn = await CommonData.LoadTableDataById<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, kitchenIssueReturnId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Kitchen Issue Return",
			TransactionNo = kitchenIssueReturn.TransactionNo,
			Action = type,
			LocationName = kitchenIssueReturn.KitchenName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = kitchenIssueReturn.TransactionNo,
				["Kitchen"] = kitchenIssueReturn.KitchenName,
				["Transaction Date"] = kitchenIssueReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = kitchenIssueReturn.TotalItems.ToString(),
				["Total Quantity"] = kitchenIssueReturn.TotalQuantity.FormatSmartDecimal(),
				["Total Amount"] = kitchenIssueReturn.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = kitchenIssueReturn.LastModifiedByUserName ?? kitchenIssueReturn.CreatedByName
			},
			Remarks = kitchenIssueReturn.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(InventoryNames.KitchenIssueReturn, kitchenIssueReturn.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = KitchenIssueReturnInvoiceExport.ExportInvoice(await KitchenIssueReturnData.LoadInvoiceBundle(kitchenIssueReturnId), InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = KitchenIssueReturnInvoiceExport.ExportInvoice(await KitchenIssueReturnData.LoadInvoiceBundle(kitchenIssueReturnId), InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
