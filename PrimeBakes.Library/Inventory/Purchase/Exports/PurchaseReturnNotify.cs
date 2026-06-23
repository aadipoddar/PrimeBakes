using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Library.Utils.Mail;
using PrimeBakes.Library.Utils.Notification;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

internal static class PurchaseReturnNotify
{
	internal static async Task Notify(int purchaseReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await PurchaseReturnNotification(purchaseReturnId, type);

		if (type != NotifyType.Created)
			await PurchaseReturnMail(purchaseReturnId, type, previousInvoice);
	}

	private static async Task PurchaseReturnNotification(int purchaseReturnId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturnId);

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Purchase Return",
			TransactionNo = purchaseReturn.TransactionNo,
			Action = type,
			LocationName = purchaseReturn.PartyName,
			Details = new Dictionary<string, string>
			{
				["🏢 Vendor"] = purchaseReturn.PartyName,
				["📦 Items"] = $"{purchaseReturn.TotalItems} | Qty: {purchaseReturn.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = purchaseReturn.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = purchaseReturn.LastModifiedByUserName ?? purchaseReturn.CreatedByName,
				["📅 Date"] = purchaseReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = purchaseReturn.Remarks
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task PurchaseReturnMail(int purchaseReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturnId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Purchase Return",
			TransactionNo = purchaseReturn.TransactionNo,
			Action = type,
			LocationName = purchaseReturn.PartyName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = purchaseReturn.TransactionNo,
				["Challan Number"] = purchaseReturn.ChallanNo,
				["Vendor"] = purchaseReturn.PartyName,
				["Transaction Date"] = purchaseReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = purchaseReturn.TotalItems.ToString(),
				["Total Quantity"] = purchaseReturn.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = purchaseReturn.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = purchaseReturn.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = purchaseReturn.LastModifiedByUserName ?? purchaseReturn.CreatedByName
			},
			Remarks = purchaseReturn.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(InventoryNames.PurchaseReturn, purchaseReturn.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturnId, InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = await PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturnId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
