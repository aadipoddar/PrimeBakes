using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Library.Utils.Mail;
using PrimeBakes.Library.Utils.Notification;

namespace PrimeBakes.Library.Store.Sale.Exports;

internal static class SaleReturnNotify
{
	internal static async Task Notify(int saleReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await SaleReturnNotification(saleReturnId, type);

		if (type != NotifyType.Created)
			await SaleReturnMail(saleReturnId, type, previousInvoice);
	}

	private static async Task SaleReturnNotification(int saleReturnId, NotifyType type)
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, saleReturnId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);

		List<UserModel> targetUsers = [];

		if (type == NotifyType.Created)
			targetUsers = [.. users.Where(u => (u.Admin || u.Store) && u.LocationId == saleReturn.LocationId)];
		else
		{
			if (saleReturn.PartyId != null && saleReturn.PartyId > 0)
			{
				var location = await LocationData.LoadLocationByLedgerId(saleReturn.PartyId.Value);

				if (location is not null)
					targetUsers = [.. users.Where(u =>
						(u.Admin || u.Store) && (
							u.LocationId == location.Id ||
							u.LocationId == 1 ||
							u.LocationId == saleReturn.LocationId))];
				else
					targetUsers = [.. users.Where(u => (u.Admin || u.Store) && (u.LocationId == 1 || u.LocationId == saleReturn.LocationId))];
			}
			else
				targetUsers = [.. users.Where(u => (u.Admin || u.Store) && (u.LocationId == 1 || u.LocationId == saleReturn.LocationId))];
		}

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Sale Return",
			TransactionNo = saleReturn.TransactionNo,
			Action = type,
			LocationName = saleReturn.LocationName,
			Details = new Dictionary<string, string>
			{
				["📍 Location"] = saleReturn.LocationName,
				["👤 Party"] = saleReturn.PartyName ?? "Walk-in Customer",
				["📦 Items"] = $"{saleReturn.TotalItems} | Qty: {saleReturn.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = saleReturn.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = saleReturn.LastModifiedByUserName ?? saleReturn.CreatedByName,
				["📅 Date"] = saleReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = saleReturn.Remarks
		};

		await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
	}

	private static async Task SaleReturnMail(int saleReturnId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, saleReturnId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Sale Return",
			TransactionNo = saleReturn.TransactionNo,
			Action = type,
			LocationName = saleReturn.LocationName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = saleReturn.TransactionNo,
				["Location"] = saleReturn.LocationName,
				["Party"] = saleReturn.PartyName ?? "Walk-in Customer",
				["Transaction Date"] = saleReturn.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = saleReturn.TotalItems.ToString(),
				["Total Quantity"] = saleReturn.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = saleReturn.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = saleReturn.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = saleReturn.LastModifiedByUserName ?? saleReturn.CreatedByName
			},
			Remarks = saleReturn.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(StoreNames.SaleReturn, saleReturn.TransactionNo)).RecordValue : null
		};

		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await SaleReturnInvoiceExport.ExportInvoice(saleReturnId, InvoiceExportType.PDF);

			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			var (pdfStream, pdfFileName) = await SaleReturnInvoiceExport.ExportInvoice(saleReturnId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
