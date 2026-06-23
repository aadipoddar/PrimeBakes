using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Library.Utils.Mail;
using PrimeBakes.Library.Utils.Notification;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

internal static class PurchaseNotify
{
	internal static async Task Notify(int purchaseId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await PurchaseNotification(purchaseId, type);

		if (type != NotifyType.Created)
			await PurchaseMail(purchaseId, type, previousInvoice);
	}

	private static async Task PurchaseNotification(int purchaseId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var purchase = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchaseId);

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Purchase",
			TransactionNo = purchase.TransactionNo,
			Action = type,
			LocationName = purchase.PartyName,
			Details = new Dictionary<string, string>
			{
				["🏢 Vendor"] = purchase.PartyName,
				["📦 Items"] = $"{purchase.TotalItems} | Qty: {purchase.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = purchase.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = purchase.LastModifiedByUserName ?? purchase.CreatedByName,
				["📅 Date"] = purchase.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = purchase.Remarks
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task PurchaseMail(int purchaseId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchaseId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Purchase",
			TransactionNo = purchase.TransactionNo,
			Action = type,
			LocationName = purchase.PartyName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = purchase.TransactionNo,
				["Challan Number"] = purchase.ChallanNo,
				["Vendor"] = purchase.PartyName,
				["Transaction Date"] = purchase.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = purchase.TotalItems.ToString(),
				["Total Quantity"] = purchase.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = purchase.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = purchase.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = purchase.LastModifiedByUserName ?? purchase.CreatedByName
			},
			Remarks = purchase.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(InventoryNames.Purchase, purchase.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await PurchaseInvoiceExport.ExportInvoice(purchaseId, InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = await PurchaseInvoiceExport.ExportInvoice(purchaseId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
