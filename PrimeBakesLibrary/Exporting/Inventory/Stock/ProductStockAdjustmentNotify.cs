using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

internal static class ProductStockAdjustmentNotify
{
    internal static async Task Notify(ProductStockModel stock, int userId, NotifyType type)
    {
        await ProductStockAdjustmentNotification(stock, userId, type);

        if (type == NotifyType.Deleted)
            await ProductStockAdjustmentMail(stock, userId);
    }

    internal static async Task Notify(int items, decimal quantity, int userId, int locationId, NotifyType type) =>
        await ProductStockAdjustmentNotification(items, quantity, userId, locationId, type);

    private static async Task ProductStockAdjustmentNotification(int items, decimal quantity, int userId, int locationId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var user = users.FirstOrDefault(_ => _.Id == userId);
        var userName = user?.Name ?? "Unknown User";

        var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId);
        var locationName = location?.Name ?? "Unknown Location";

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Product Stock Adjustment",
            TransactionNo = null,
            Action = type,
            LocationName = locationName,
            Details = new Dictionary<string, string>
            {
                ["📍 Location"] = locationName,
                ["📦 Items"] = items.ToString(),
                ["🔢 Quantity"] = quantity.FormatSmartDecimal(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "Adjusted By")] = userName
            },
            Remarks = null
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task ProductStockAdjustmentNotification(ProductStockModel stock, int userId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var user = users.FirstOrDefault(_ => _.Id == userId);
        var userName = user?.Name ?? "Unknown User";

        var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, stock.LocationId);
        var locationName = location?.Name ?? "Unknown Location";

        var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, stock.ProductId);
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
                ["📅 Date"] = stock.TransactionDate.ToString("dd MMM yyyy"),
                ["👤 Deleted By"] = userName
            },
            Remarks = null
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task ProductStockAdjustmentMail(ProductStockModel stock, int userId)
    {
        var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, userId);
        var userName = user?.Name ?? "Unknown User";

        var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, stock.LocationId);
        var locationName = location?.Name ?? "Unknown Location";

        var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, stock.ProductId);
        var productName = product?.Name ?? "Unknown Product";
        var productCode = product?.Code ?? "N/A";

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Product Stock Adjustment",
            TransactionNo = stock.TransactionNo,
            Action = NotifyType.Deleted,
            LocationName = locationName,
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = stock.TransactionNo ?? "N/A",
                ["Transaction Date"] = stock.TransactionDate.ToString("dd MMM yyyy"),
                ["Location"] = locationName,
                ["Product"] = productName,
                ["Code"] = productCode,
                ["Quantity Deleted"] = stock.Quantity.FormatSmartDecimal(),
                ["Deleted By"] = userName
            },
            Remarks = null
        };

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
