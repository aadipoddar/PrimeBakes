using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

internal static class RawMaterialStockAdjustmentNotify
{
    internal static async Task Notify(RawMaterialStockModel stock, int userId, NotifyType type)
    {
        await RawMaterialStockAdjustmentNotification(stock, userId, type);

        if (type == NotifyType.Deleted)
            await RawMaterialStockAdjustmentMail(stock, userId);
    }

    internal static async Task Notify(int items, decimal quantity, int userId, NotifyType type) =>
        await RawMaterialStockAdjustmentNotification(items, quantity, userId, type);

    private static async Task RawMaterialStockAdjustmentNotification(int items, decimal quantity, int userId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var user = users.FirstOrDefault(_ => _.Id == userId);
        var userName = user?.Name ?? "Unknown User";

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Raw Material Stock Adjustment",
            TransactionNo = null,
            Action = type,
            LocationName = "Main Location",
            Details = new Dictionary<string, string>
            {
                ["📦 Items"] = items.ToString(),
                ["🔢 Quantity"] = quantity.FormatSmartDecimal(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "Adjusted By")] = userName
            },
            Remarks = null
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task RawMaterialStockAdjustmentNotification(RawMaterialStockModel stock, int userId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var user = users.FirstOrDefault(_ => _.Id == userId);
        var userName = user?.Name ?? "Unknown User";

        var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, stock.RawMaterialId);
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
                ["📅 Date"] = stock.TransactionDate.ToString("dd MMM yyyy"),
                ["👤 Deleted By"] = userName
            },
            Remarks = null
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task RawMaterialStockAdjustmentMail(RawMaterialStockModel stock, int userId)
    {
        var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, userId);
        var userName = user?.Name ?? "Unknown User";

        var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, stock.RawMaterialId);
        var rawMaterialName = rawMaterial?.Name ?? "Unknown Material";
        var rawMaterialCode = rawMaterial?.Code ?? "N/A";
        var uom = rawMaterial?.UnitOfMeasurement ?? "N/A";

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Raw Material Stock Adjustment",
            TransactionNo = stock.TransactionNo,
            Action = NotifyType.Deleted,
            LocationName = "Main Location",
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = stock.TransactionNo ?? "N/A",
                ["Transaction Date"] = stock.TransactionDate.ToString("dd MMM yyyy"),
                ["Raw Material"] = rawMaterialName,
                ["Code"] = rawMaterialCode,
                ["Unit of Measurement"] = uom,
                ["Quantity Deleted"] = stock.Quantity.FormatSmartDecimal(),
                ["Deleted By"] = userName
            },
            Remarks = null
        };

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
