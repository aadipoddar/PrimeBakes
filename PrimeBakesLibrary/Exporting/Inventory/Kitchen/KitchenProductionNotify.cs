using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

internal static class KitchenProductionNotify
{
    internal static async Task Notify(int kitchenProductionId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        await KitchenProductionNotification(kitchenProductionId, type);

        if (type != NotifyType.Created)
            await KitchenProductionMail(kitchenProductionId, type, previousInvoice);
    }

    private static async Task KitchenProductionNotification(int kitchenProductionId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(ViewNames.KitchenProductionOverview, kitchenProductionId);

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Kitchen Production",
            TransactionNo = kitchenProduction.TransactionNo,
            Action = type,
            LocationName = kitchenProduction.KitchenName,
            Details = new Dictionary<string, string>
            {
                ["🍳 Kitchen"] = kitchenProduction.KitchenName,
                ["📦 Items"] = $"{kitchenProduction.TotalItems} | Qty: {kitchenProduction.TotalQuantity.FormatSmartDecimal()}",
                ["💰 Amount"] = kitchenProduction.TotalAmount.FormatIndianCurrency(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = kitchenProduction.LastModifiedByUserName ?? kitchenProduction.CreatedByName,
                ["📅 Date"] = kitchenProduction.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
            },
            Remarks = kitchenProduction.Remarks
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task KitchenProductionMail(int kitchenProductionId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(ViewNames.KitchenProductionOverview, kitchenProductionId);

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Kitchen Production",
            TransactionNo = kitchenProduction.TransactionNo,
            Action = type,
            LocationName = kitchenProduction.KitchenName,
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = kitchenProduction.TransactionNo,
                ["Kitchen"] = kitchenProduction.KitchenName,
                ["Transaction Date"] = kitchenProduction.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
                ["Total Items"] = kitchenProduction.TotalItems.ToString(),
                ["Total Quantity"] = kitchenProduction.TotalQuantity.FormatSmartDecimal(),
                ["Total Amount"] = kitchenProduction.TotalAmount.FormatIndianCurrency(),
                [type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = kitchenProduction.LastModifiedByUserName ?? kitchenProduction.CreatedByName
            },
            Remarks = kitchenProduction.Remarks
        };

        // For update emails, include before and after invoices
        if (type == NotifyType.Updated && previousInvoice.HasValue)
        {
            var (afterStream, afterFileName) = await KitchenProductionInvoiceExport.ExportInvoice(kitchenProductionId, InvoiceExportType.PDF);

            // Rename files to make it clear which is which
            var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
            var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

            emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
            emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
        }
        else
        {
            // For delete/recover, just attach the current invoice
            var (pdfStream, pdfFileName) = await KitchenProductionInvoiceExport.ExportInvoice(kitchenProductionId, InvoiceExportType.PDF);
            emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
        }

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
