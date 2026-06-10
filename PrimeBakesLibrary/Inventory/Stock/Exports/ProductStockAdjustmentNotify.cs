using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Utils.Mail;
using PrimeBakesLibrary.Utils.Notification;

namespace PrimeBakesLibrary.Inventory.Stock.Exports;

internal static class ProductStockAdjustmentNotify
{
	internal static async Task NotifyDeleted(ProductStockModel stock, int userId, NotifyType type)
	{
		await ProductStockDeletedNotification(stock, userId, type);

		if (type != NotifyType.Created)
			await ProductStockAdjustmentMail(stock, userId);
	}

	internal static async Task NotifyCreated(int items, decimal quantity, string transactionNo, int userId, int locationId, NotifyType type) =>
		await ProductStockCreatedNotification(items, quantity, transactionNo, userId, locationId, type);

	private static async Task ProductStockCreatedNotification(int items, decimal quantity, string transactionNo, int userId, int locationId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var user = users.FirstOrDefault(_ => _.Id == userId);
		var userName = user?.Name ?? "Unknown User";

		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId);
		var locationName = location?.Name ?? "Unknown Location";

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Product Stock Adjustment",
			TransactionNo = transactionNo,
			Action = type,
			LocationName = locationName,
			Details = new Dictionary<string, string>
			{
				["📍 Location"] = locationName,
				["📦 Items"] = $"{items} | Qty: {quantity.FormatSmartDecimal()}",
				["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = userName
			},
			Remarks = null
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task ProductStockDeletedNotification(ProductStockModel stock, int userId, NotifyType type)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var user = users.FirstOrDefault(_ => _.Id == userId);
		var userName = user?.Name ?? "Unknown User";

		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, stock.LocationId);
		var locationName = location?.Name ?? "Unknown Location";

		var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, stock.ProductId);
		var productName = product?.Name ?? "Unknown Product";

		var notificationData = new NotificationUtil.TransactionNotificationData
		{
			TransactionType = "Product Stock Adjustment",
			TransactionNo = stock.TransactionNo,
			Action = type,
			LocationName = locationName,
			Details = new Dictionary<string, string>
			{
				["📍 Location"] = locationName,
				["📦 Item"] = productName,
				["🔢 Quantity"] = stock.Quantity.FormatSmartDecimal(),
				["📅 Date"] = stock.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["👤 Deleted By"] = userName
			},
			Remarks = null
		};

		await NotificationUtil.SendTransactionNotification(users, notificationData);
	}

	private static async Task ProductStockAdjustmentMail(ProductStockModel stock, int userId)
	{
		var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId);
		var userName = user?.Name ?? "Unknown User";

		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, stock.LocationId);
		var locationName = location?.Name ?? "Unknown Location";

		var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, stock.ProductId);
		var productName = product?.Name ?? "Unknown Product";
		var productCode = product?.Code ?? "N/A";

		var emailData = new TransactionMailing.TransactionEmailData
		{
			TransactionType = "Product Stock Adjustment",
			TransactionNo = stock.TransactionNo,
			Action = NotifyType.Deleted,
			LocationName = locationName,
			Details = new Dictionary<string, string>
			{
				["Transaction Number"] = stock.TransactionNo ?? "N/A",
				["Transaction Date"] = stock.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
				["Location"] = locationName,
				["Product"] = productName,
				["Code"] = productCode,
				["Quantity Deleted"] = stock.Quantity.FormatSmartDecimal(),
				["Deleted By"] = userName
			},
			Remarks = null
		};

		await TransactionMailing.SendTransactionEmail(emailData);
	}
}
