using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;
using PrimeBakesLibrary.Utils.Notification;

namespace PrimeBakesLibrary.Store.StockTransfer.Exports;

internal static class StockTransferNotify
{
	internal static async Task Notify(int stockTransferId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await StockTransferNotification(stockTransferId, type);

		if (type != NotifyType.Created)
			await StockTransferMail(stockTransferId, type, previousInvoice);
	}

	private static async Task StockTransferNotification(int stockTransferId, NotifyType type)
	{
		var stockTransfer = await CommonData.LoadTableDataById<StockTransferOverviewModel>(StoreNames.StockTransferOverview, stockTransferId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);

		List<UserModel> targetUsers = [];

		if (type == NotifyType.Created)
			targetUsers = [.. users.Where(u => (u.Admin || u.Store) && (u.LocationId == stockTransfer.LocationId || u.LocationId == stockTransfer.ToLocationId))];
		else
			targetUsers = [.. users.Where(u => (u.Admin || u.Store) && (u.LocationId == 1 || u.LocationId == stockTransfer.LocationId || u.LocationId == stockTransfer.ToLocationId))];

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Stock Transfer",
			TransactionNo = stockTransfer.TransactionNo,
			Action = type,
			LocationName = $"{stockTransfer.LocationName} → {stockTransfer.ToLocationName}",
			Details = new Dictionary<string, string>
			{
				["📤 From"] = stockTransfer.LocationName,
				["📥 To"] = stockTransfer.ToLocationName,
				["📦 Items"] = $"{stockTransfer.TotalItems} | Qty: {stockTransfer.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = stockTransfer.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = stockTransfer.LastModifiedByUserName ?? stockTransfer.CreatedByName,
				["📅 Date"] = stockTransfer.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = stockTransfer.Remarks
		};

		await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
	}

	private static async Task StockTransferMail(int stockTransferId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var stockTransfer = await CommonData.LoadTableDataById<StockTransferOverviewModel>(StoreNames.StockTransferOverview, stockTransferId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Stock Transfer",
			TransactionNo = stockTransfer.TransactionNo,
			Action = type,
			LocationName = $"{stockTransfer.LocationName} → {stockTransfer.ToLocationName}",
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = stockTransfer.TransactionNo,
				["From Location"] = stockTransfer.LocationName,
				["To Location"] = stockTransfer.ToLocationName,
				["Transaction Date"] = stockTransfer.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = stockTransfer.TotalItems.ToString(),
				["Total Quantity"] = stockTransfer.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = stockTransfer.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = stockTransfer.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = stockTransfer.LastModifiedByUserName ?? stockTransfer.CreatedByName
			},
			Remarks = stockTransfer.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(StoreNames.StockTransfer, stockTransfer.TransactionNo)).RecordValue : null
		};

		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await StockTransferInvoiceExport.ExportInvoice(stockTransferId, InvoiceExportType.PDF);

			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			var (pdfStream, pdfFileName) = await StockTransferInvoiceExport.ExportInvoice(stockTransferId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
