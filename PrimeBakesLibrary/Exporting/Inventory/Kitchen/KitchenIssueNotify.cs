using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

internal static class KitchenIssueNotify
{
    internal static async Task Notify(int kitchenIssueId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        await KitchenIssueNotification(kitchenIssueId, type);

        if (type != NotifyType.Created)
            await KitchenIssueMail(kitchenIssueId, type, previousInvoice);
    }

    private static async Task KitchenIssueNotification(int kitchenIssueId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(ViewNames.KitchenIssueOverview, kitchenIssueId);

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Kitchen Issue",
            TransactionNo = kitchenIssue.TransactionNo,
            Action = type,
            LocationName = kitchenIssue.KitchenName,
            Details = new Dictionary<string, string>
            {
                ["🍳 Kitchen"] = kitchenIssue.KitchenName,
                ["📦 Items"] = $"{kitchenIssue.TotalItems} | Qty: {kitchenIssue.TotalQuantity.FormatSmartDecimal()}",
                ["💰 Amount"] = kitchenIssue.TotalAmount.FormatIndianCurrency(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = kitchenIssue.LastModifiedByUserName ?? kitchenIssue.CreatedByName,
                ["📅 Date"] = kitchenIssue.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
            },
            Remarks = kitchenIssue.Remarks
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task KitchenIssueMail(int kitchenIssueId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(ViewNames.KitchenIssueOverview, kitchenIssueId);

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Kitchen Issue",
            TransactionNo = kitchenIssue.TransactionNo,
            Action = type,
            LocationName = kitchenIssue.KitchenName,
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = kitchenIssue.TransactionNo,
                ["Kitchen"] = kitchenIssue.KitchenName,
                ["Transaction Date"] = kitchenIssue.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
                ["Total Items"] = kitchenIssue.TotalItems.ToString(),
                ["Total Quantity"] = kitchenIssue.TotalQuantity.FormatSmartDecimal(),
                ["Total Amount"] = kitchenIssue.TotalAmount.FormatIndianCurrency(),
                [type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = kitchenIssue.LastModifiedByUserName ?? kitchenIssue.CreatedByName
            },
            Remarks = kitchenIssue.Remarks
        };

        // For update emails, include before and after invoices
        if (type == NotifyType.Updated && previousInvoice.HasValue)
        {
            var (afterStream, afterFileName) = await KitchenIssueInvoiceExport.ExportInvoice(kitchenIssueId, InvoiceExportType.PDF);

            // Rename files to make it clear which is which
            var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
            var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

            emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
            emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
        }
        else
        {
            // For delete/recover, just attach the current invoice
            var (pdfStream, pdfFileName) = await KitchenIssueInvoiceExport.ExportInvoice(kitchenIssueId, InvoiceExportType.PDF);
            emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
        }

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
