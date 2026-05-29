using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Utils.ExportUtils;
using PrimeBakesLibrary.Utils.MailUtils;
using PrimeBakesLibrary.Utils.NotificationUtils;
using PrimeBakesLibrary.Operations.User.Models;
using PrimeBakesLibrary.Store.Order.Models;

namespace PrimeBakesLibrary.Store.Order.Exports;

internal static class OrderNotify
{
    internal static async Task Notify(int orderId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        await OrderNotification(orderId, type);

        if (type != NotifyType.Created)
            await OrderMail(orderId, type, previousInvoice);
    }

    private static async Task OrderNotification(int orderId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Store && u.LocationId == 1)];

        var order = await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, orderId);

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Order",
            TransactionNo = order.TransactionNo,
            Action = type,
            LocationName = order.LocationName,
            Details = new Dictionary<string, string>
            {
                ["📍 Outlet"] = order.LocationName,
                ["📦 Items"] = $"{order.TotalItems} | Qty: {order.TotalQuantity.FormatSmartDecimal()}",
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = order.LastModifiedByUserName ?? order.CreatedByName,
                ["📅 Date"] = order.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
            },
            Remarks = order.Remarks
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task OrderMail(int orderId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        var order = await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, orderId);

        var emailData = new TransactionMailing.TransactionEmailData
        {
            TransactionType = "Order",
            TransactionNo = order.TransactionNo,
            Action = type,
            LocationName = order.LocationName,
            Details = new Dictionary<string, string>
            {
                ["Order Number"] = order.TransactionNo,
                ["Outlet"] = order.LocationName,
                ["Order Date"] = order.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
                ["Total Items"] = order.TotalItems.ToString(),
                ["Total Quantity"] = order.TotalQuantity.FormatSmartDecimal(),
                [type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = order.LastModifiedByUserName ?? order.CreatedByName
            },
            Remarks = order.Remarks
        };

        // For update emails, include before and after invoices
        if (type == NotifyType.Updated && previousInvoice.HasValue)
        {
            var (afterStream, afterFileName) = await OrderInvoiceExport.ExportInvoice(orderId, InvoiceExportType.PDF);

            // Rename files to make it clear which is which
            var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
            var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

            emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
            emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
        }
        else
        {
            // For delete/recover, just attach the current invoice
            var (pdfStream, pdfFileName) = await OrderInvoiceExport.ExportInvoice(orderId, InvoiceExportType.PDF);
            emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
        }

        await TransactionMailing.SendTransactionEmail(emailData);
    }
}
