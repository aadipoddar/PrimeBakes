using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Exports.Inventory.PurchaseOrder;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.PurchaseOrder;

internal static class PurchaseOrderNotify
{
	internal static async Task Notify(int purchaseOrderId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await PurchaseOrderNotification(purchaseOrderId, type);
		await PurchaseOrderMail(purchaseOrderId, type, previousInvoice);
	}

	private static async Task PurchaseOrderNotification(int purchaseOrderId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderOverviewModel>(InventoryNames.PurchaseOrderOverview, purchaseOrderId);

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Purchase Order",
			TransactionNo = purchaseOrder.TransactionNo,
			Action = type,
			LocationName = purchaseOrder.PartyName,
			Details = new Dictionary<string, string>
			{
				["🏢 Vendor"] = purchaseOrder.PartyName,
				["📦 Items"] = $"{purchaseOrder.TotalItems} | Qty: {purchaseOrder.TotalQuantity.FormatSmartDecimal()}",
				["🚚 Expected"] = purchaseOrder.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "Not specified",
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = purchaseOrder.LastModifiedByUserName ?? purchaseOrder.CreatedByName,
				["📅 Date"] = purchaseOrder.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = purchaseOrder.Remarks
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task PurchaseOrderMail(int purchaseOrderId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderOverviewModel>(InventoryNames.PurchaseOrderOverview, purchaseOrderId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Purchase Order",
			TransactionNo = purchaseOrder.TransactionNo,
			Action = type,
			LocationName = purchaseOrder.PartyName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = purchaseOrder.TransactionNo,
				["Vendor"] = purchaseOrder.PartyName,
				["Transaction Date"] = purchaseOrder.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Expected Delivery"] = purchaseOrder.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "Not specified",
				["Purchase Number"] = purchaseOrder.PurchaseTransactionNo ?? "Not yet received",
				["Total Items"] = purchaseOrder.TotalItems.ToString(),
				["Total Quantity"] = purchaseOrder.TotalQuantity.FormatSmartDecimal(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = purchaseOrder.LastModifiedByUserName ?? purchaseOrder.CreatedByName
			},
			Remarks = purchaseOrder.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(InventoryNames.PurchaseOrder, purchaseOrder.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = PurchaseOrderInvoiceExport.ExportInvoice(await PurchaseOrderData.LoadInvoiceBundle(purchaseOrderId), InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = PurchaseOrderInvoiceExport.ExportInvoice(await PurchaseOrderData.LoadInvoiceBundle(purchaseOrderId), InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
