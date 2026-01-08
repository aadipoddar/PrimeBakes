using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

internal static class SaleNotify
{
    internal static async Task Notify(int saleId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        await SaleNotification(saleId, type);

        if (type != NotifyType.Created)
            await SaleMail(saleId, type, previousInvoice);
    }

    private static async Task SaleNotification(int saleId, NotifyType type)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);

        List<UserModel> targetUsers = [];

        // For Save (new sale creation)
        if (type == NotifyType.Created)
            // Only notify sales and admins of the outlet where the sale was made
            targetUsers = [.. users.Where(u => (u.Admin || u.Sales) && u.LocationId == sale.LocationId)];

        // For Delete, Recover, or Update operations
        else
        {
            if (sale.PartyId != null && sale.PartyId > 0)
            {
                var location = await LocationData.LoadLocationByLedgerId(sale.PartyId.Value);

                // If party has a valid location
                if (location is not null)
                    // Notify sales and admins of:
                    // 1. The party outlet (where sale was made to)
                    // 2. The main outlet (LocationId = 1)
                    // 3. The outlet where the sale originated from (sale.LocationId)
                    targetUsers = [.. users.Where(u =>
                        (u.Admin || u.Sales) && (
                            u.LocationId == location.Id ||          // Party outlet
                            u.LocationId == 1 ||                    // Main outlet
                            u.LocationId == sale.LocationId         // Originating outlet
                        ))];
                else
                    // If party doesn't have a location, notify sales and admins of the originating outlet and main outlet
                    targetUsers = [.. users.Where(u =>
                        (u.Admin || u.Sales) && (
                            u.LocationId == sale.LocationId ||      // Originating outlet
                            u.LocationId == 1                       // Main outlet
                        ))];
            }
            else
                // If no party, notify sales and admins of the originating outlet and main outlet
                targetUsers = [.. users.Where(u =>
                    (u.Admin || u.Sales) && (
                        u.LocationId == sale.LocationId ||          // Originating outlet
                        u.LocationId == 1                           // Main outlet
                    ))];
        }

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Sale",
            TransactionNo = sale.TransactionNo,
            Action = type,
            LocationName = sale.LocationName,
            Details = new Dictionary<string, string>
            {
                ["📍 Location"] = sale.LocationName,
                ["👤 Party"] = sale.PartyName ?? "Walk-in Customer",
                ["📦 Items"] = $"{sale.TotalItems} | Qty: {sale.TotalQuantity.FormatSmartDecimal()}",
                ["💰 Amount"] = sale.TotalAmount.FormatIndianCurrency(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = sale.LastModifiedByUserName ?? sale.CreatedByName,
                ["📅 Date"] = sale.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
            },
            Remarks = sale.Remarks
        };

        await NotificationUtil.SendTransactionNotification(targetUsers, notificationData);
    }

    private static async Task SaleMail(int saleId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Sale",
            TransactionNo = sale.TransactionNo,
            Action = type,
            LocationName = sale.LocationName,
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = sale.TransactionNo,
                ["Location"] = sale.LocationName,
                ["Party"] = sale.PartyName ?? "Walk-in Customer",
                ["Transaction Date"] = sale.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
                ["Total Items"] = sale.TotalItems.ToString(),
                ["Total Quantity"] = sale.TotalQuantity.FormatSmartDecimal(),
                ["Base Total"] = sale.BaseTotal.FormatIndianCurrency(),
                ["Total Amount"] = sale.TotalAmount.FormatIndianCurrency(),
                [type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = sale.LastModifiedByUserName ?? sale.CreatedByName
            },
            Remarks = sale.Remarks
        };

        if (type == NotifyType.Updated && previousInvoice.HasValue)
        {
            var (afterStream, afterFileName) = await SaleInvoiceExport.ExportInvoice(saleId, InvoiceExportType.PDF);

            var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
            var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

            emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
            emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
        }
        else
        {
            var (pdfStream, pdfFileName) = await SaleInvoiceExport.ExportInvoice(saleId, InvoiceExportType.PDF);
            emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
        }

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
