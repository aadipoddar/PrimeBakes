using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Masters;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

internal static class BillNotify
{
	internal static async Task Notify(int billId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		await BillNotification(billId, type);

		if (type != NotifyType.Created)
			await BillMail(billId, type, previousInvoice);
	}

	private static async Task BillNotification(int billId, NotifyType type)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		CustomerModel customer = null;
		if (bill.CustomerId.HasValue && bill.CustomerId.Value > 0)
			customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, bill.CustomerId.Value);

		var createdBy = users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name ?? "Unknown";
		var modifiedBy = bill.LastModifiedBy.HasValue
			? users.FirstOrDefault(u => u.Id == bill.LastModifiedBy.Value)?.Name
			: null;

		List<UserModel> targetUsers;

		if (type == NotifyType.Created)
		{
			// On create, notify restaurant/admin users in the same outlet.
			targetUsers = [.. users.Where(u => (u.Admin || u.Restaurant) && u.LocationId == bill.LocationId)];
		}
		else
		{
			// On update/delete/recover, also include main outlet (Location 1).
			targetUsers = [.. users.Where(u => (u.Admin || u.Restaurant) && (u.LocationId == bill.LocationId || u.LocationId == 1))];
		}

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Bill",
			TransactionNo = bill.TransactionNo,
			Action = type,
			LocationName = location?.Name ?? "Unknown Outlet",
			Details = new Dictionary<string, string>
			{
				["📍 Outlet"] = location?.Name ?? "Unknown",
				["🍽️ Table"] = table?.Name ?? "N/A",
				["👤 Customer"] = customer?.Name ?? "Walk-in Customer",
				["📦 Items"] = $"{bill.TotalItems} | Qty: {bill.TotalQuantity.FormatSmartDecimal()}",
				["💰 Amount"] = bill.TotalAmount.FormatIndianCurrency(),
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = modifiedBy ?? createdBy,
				["📅 Date"] = bill.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
			},
			Remarks = bill.Remarks
		};

		await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
	}

	private static async Task BillMail(int billId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		CustomerModel customer = null;
		if (bill.CustomerId.HasValue && bill.CustomerId.Value > 0)
			customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, bill.CustomerId.Value);

		var createdBy = users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name ?? "Unknown";
		var modifiedBy = bill.LastModifiedBy.HasValue
			? users.FirstOrDefault(u => u.Id == bill.LastModifiedBy.Value)?.Name
			: null;

		var emailData = new MailingUtil.TransactionEmailData
		{
			TransactionType = "Bill",
			TransactionNo = bill.TransactionNo,
			Action = type,
			LocationName = location?.Name ?? "Unknown Outlet",
			Details = new Dictionary<string, string>
			{
				["Bill Number"] = bill.TransactionNo,
				["Outlet"] = location?.Name ?? "Unknown",
				["Dining Table"] = table?.Name ?? "N/A",
				["Customer"] = customer?.Name ?? "Walk-in Customer",
				["Transaction Date"] = bill.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Total Items"] = bill.TotalItems.ToString(),
				["Total Quantity"] = bill.TotalQuantity.FormatSmartDecimal(),
				["Base Total"] = bill.BaseTotal.FormatIndianCurrency(),
				["Total Amount"] = bill.TotalAmount.FormatIndianCurrency(),
				[type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = modifiedBy ?? createdBy
			},
			Remarks = bill.Remarks
		};

		if (type == NotifyType.Updated && previousInvoice.HasValue)
		{
			var (afterStream, afterFileName) = await BillInvoiceExport.ExportInvoice(billId, InvoiceExportType.PDF);

			var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
			var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

			emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
			emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
		}
		else
		{
			var (pdfStream, pdfFileName) = await BillInvoiceExport.ExportInvoice(billId, InvoiceExportType.PDF);
			emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
		}

		await MailingUtil.SendTransactionEmail(emailData);
	}
}
