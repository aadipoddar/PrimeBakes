using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Exports.Restaurant.Bill;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Data.Restaurant.Bill;

internal static class BillNotify
{
	internal static async Task Notify(int billId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await BillNotification(billId, type);
		await BillMail(billId, type, previousInvoice);
	}

	internal static async Task NotifyDayClosing(
		DateTime postingDate,
		int locationId,
		int totalBills,
		decimal totalAmount,
		decimal totalExtraTaxAmount,
		int userId,
		string transactionNo)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId);
		var userName = users.FirstOrDefault(u => u.Id == userId)?.Name ?? "Unknown";

		List<UserModel> targetUsers = [.. users.Where(u => (u.Admin || u.Restaurant) && (u.LocationId == locationId || u.LocationId == 1))];

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Bill Day Closing",
			TransactionNo = string.IsNullOrWhiteSpace(transactionNo) ? "N/A" : transactionNo,
			Action = NotifyType.Created,
			LocationName = location?.Name ?? "Unknown Outlet",
			Details = new Dictionary<string, string>
			{
				["📍 Outlet"] = location?.Name ?? "Unknown",
				["📅 Date"] = $"{postingDate:dd MMM yyyy}",
				["🧾 Bills"] = totalBills.ToString(),
				["💰 Total Amount"] = totalAmount.FormatIndianCurrency(),
				["🧮 Extra Tax"] = totalExtraTaxAmount.FormatIndianCurrency(),
				["👤 By"] = userName
			},
			Remarks = $"Bill day closing completed for {postingDate:dd-MMM-yyyy}"
		};

		await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
	}

	private static async Task BillNotification(int billId, NotifyType type)
	{
		var bill = await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, billId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);

		List<UserModel> targetUsers = [.. users.Where(u => (u.Admin || u.Restaurant) && (u.LocationId == bill.LocationId || u.LocationId == 1))];

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Bill",
			TransactionNo = bill.TransactionNo,
			Action = type,
			LocationName = bill.LocationName,
			Details = new Dictionary<string, string>
			{
				["📍 Outlet"] = bill.LocationName,
				["🍽️ Table"] = bill.DiningTableName ?? "N/A",
				["👤 Customer"] = bill.CustomerName ?? "Walk-in Customer",
				["📦 Items"] = $"{bill.TotalItems} | Qty: {bill.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = bill.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = bill.LastModifiedByUserName ?? bill.CreatedByName,
				["📅 Date"] = bill.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = bill.Remarks
		};

		await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
	}

	private static async Task BillMail(int billId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var bill = await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, billId);

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Bill",
			TransactionNo = bill.TransactionNo,
			Action = type,
			LocationName = bill.LocationName,
			Details = new Dictionary<string, string>
			{
				["Bill Number"] = bill.TransactionNo,
				["Outlet"] = bill.LocationName,
				["Dining Table"] = bill.DiningTableName ?? "N/A",
				["Customer"] = bill.CustomerName ?? "Walk-in Customer",
				["Transaction Date"] = bill.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = bill.TotalItems.ToString(),
				["Total Quantity"] = bill.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = bill.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = bill.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = bill.LastModifiedByUserName ?? bill.CreatedByName
			},
			Remarks = bill.Remarks,
			Differences = type == NotifyType.Updated ? (await AuditTrailData.LoadLastAuditTrailByTableRecord(RestaurantNames.Bill, bill.TransactionNo)).RecordValue : null
		};

		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = BillInvoiceExport.ExportInvoice(await BillData.LoadInvoiceBundle(billId), InvoiceExportType.PDF);

			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			var (pdfStream, pdfFileName) = BillInvoiceExport.ExportInvoice(await BillData.LoadInvoiceBundle(billId), InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
