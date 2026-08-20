using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Exports.Inventory.Kitchen;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;

internal static class KitchenProductionReturnNotify
{
	internal static async Task Notify(int kitchenProductionReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await KitchenProductionReturnNotification(kitchenProductionReturnId, type);

		if (type != NotifyType.Created)
			await KitchenProductionReturnMail(kitchenProductionReturnId, type, previousInvoice);
	}

	private static async Task KitchenProductionReturnNotification(int kitchenProductionReturnId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var kitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, kitchenProductionReturnId);

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Kitchen Production Return",
			TransactionNo = kitchenProductionReturn.TransactionNo,
			Action = type,
			LocationName = kitchenProductionReturn.KitchenName,
			Details = new Dictionary<string, string>
			{
				["🍳 Kitchen"] = kitchenProductionReturn.KitchenName,
				["📦 Items"] = $"{kitchenProductionReturn.TotalItems} | Qty: {kitchenProductionReturn.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = kitchenProductionReturn.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = kitchenProductionReturn.LastModifiedByUserName ?? kitchenProductionReturn.CreatedByName,
				["📅 Date"] = kitchenProductionReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = kitchenProductionReturn.Remarks
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task KitchenProductionReturnMail(int kitchenProductionReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var kitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, kitchenProductionReturnId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Kitchen Production Return",
			TransactionNo = kitchenProductionReturn.TransactionNo,
			Action = type,
			LocationName = kitchenProductionReturn.KitchenName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = kitchenProductionReturn.TransactionNo,
				["Kitchen"] = kitchenProductionReturn.KitchenName,
				["Transaction Date"] = kitchenProductionReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = kitchenProductionReturn.TotalItems.ToString(),
				["Total Quantity"] = kitchenProductionReturn.TotalQuantity.FormatSmartDecimal(),
				["Total Amount"] = kitchenProductionReturn.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = kitchenProductionReturn.LastModifiedByUserName ?? kitchenProductionReturn.CreatedByName
			},
			Remarks = kitchenProductionReturn.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(InventoryNames.KitchenProductionReturn, kitchenProductionReturn.TransactionNo)).RecordValue : null
		};

		// For update emails, include before and after invoices
		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = KitchenProductionReturnInvoiceExport.ExportInvoice(await KitchenProductionReturnData.LoadInvoiceBundle(kitchenProductionReturnId), InvoiceExportType.PDF);

			// Rename files to make it clear which is which
			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			// For delete/recover, just attach the current invoice
			var (pdfStream, pdfFileName) = KitchenProductionReturnInvoiceExport.ExportInvoice(await KitchenProductionReturnData.LoadInvoiceBundle(kitchenProductionReturnId), InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
