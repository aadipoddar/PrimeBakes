using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Mail;
using PrimeBakesLibrary.Utils.Notification;

namespace PrimeBakesLibrary.Inventory.Stock.Exports;

internal static class RawMaterialStockAdjustmentNotify
{
	internal static async Task NotifyDeleted(RawMaterialStockModel stock, int userId, NotifyType type)
	{
		await RawMaterialStockDeletedNotification(stock, userId, type);

		if (type != NotifyType.Created)
			await RawMaterialStockAdjustmentMail(stock, userId);
	}

	internal static async Task NotifyCreated(int items, decimal quantity, string transactionNo, int userId, NotifyType type) =>
		await RawMaterialStockCreatedNotification(items, quantity, transactionNo, userId, type);

	private static async Task RawMaterialStockCreatedNotification(int items, decimal quantity, string transactionNo, int userId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var user = users.FirstOrDefault(_ => _.Id == userId);
		var userName = user?.Name ?? "Unknown User";

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Raw Material Stock Adjustment",
			TransactionNo = transactionNo,
			Action = type,
			LocationName = "Main Location",
			Details = new Dictionary<string, string>
			{
				["📦 Items"] = $"{items} | Qty: {quantity.FormatSmartDecimal()}",
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = userName
			},
			Remarks = null
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task RawMaterialStockDeletedNotification(RawMaterialStockModel stock, int userId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var user = users.FirstOrDefault(_ => _.Id == userId);
		var userName = user?.Name ?? "Unknown User";

		var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(InventoryNames.RawMaterial, stock.RawMaterialId);
		var rawMaterialName = rawMaterial?.Name ?? "Unknown Material";

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Raw Material Stock Adjustment",
			TransactionNo = stock.TransactionNo,
			Action = type,
			LocationName = "Main Location",
			Details = new Dictionary<string, string>
			{
				["📦 Item"] = rawMaterialName,
				["🔢 Quantity"] = stock.Quantity.FormatSmartDecimal(),
				["📅 Date"] = stock.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["👤 Deleted By"] = userName
			},
			Remarks = null
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task RawMaterialStockAdjustmentMail(RawMaterialStockModel stock, int userId)
	{
		var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId);
		var userName = user?.Name ?? "Unknown User";

		var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(InventoryNames.RawMaterial, stock.RawMaterialId);
		var rawMaterialName = rawMaterial?.Name ?? "Unknown Material";
		var rawMaterialCode = rawMaterial?.Code ?? "N/A";
		var uom = rawMaterial?.UnitOfMeasurement ?? "N/A";

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Raw Material Stock Adjustment",
			TransactionNo = stock.TransactionNo,
			Action = NotifyType.Deleted,
			LocationName = "Main Location",
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = stock.TransactionNo ?? "N/A",
				["Transaction Date"] = stock.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Raw Material"] = rawMaterialName,
				["Code"] = rawMaterialCode,
				["Unit of Measurement"] = uom,
				["Quantity Deleted"] = stock.Quantity.FormatSmartDecimal(),
				["Deleted By"] = userName
			},
			Remarks = null
		};

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
