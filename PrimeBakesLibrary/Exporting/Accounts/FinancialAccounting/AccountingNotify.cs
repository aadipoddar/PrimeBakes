using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

internal static class AccountingNotify
{
    internal static async Task Notify(int accountingId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        if (type == NotifyType.Created)
            return;

        await AccountingNotification(accountingId, type);
        await AccountingMail(accountingId, type, previousInvoice);
    }

    private static async Task AccountingNotification(int accountingId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Accounts && u.LocationId == 1)];

        var accounting = await CommonData.LoadTableDataById<AccountingOverviewModel>(ViewNames.AccountingOverview, accountingId);

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Accounting",
            TransactionNo = accounting.TransactionNo,
            Action = type,
            LocationName = accounting.VoucherName,
            Details = new Dictionary<string, string>
            {
                ["📋 Voucher"] = accounting.VoucherName,
                ["📊 Ledgers"] = $"Dr: {accounting.TotalDebitLedgers} | Cr: {accounting.TotalCreditLedgers}",
                ["💰 Amount"] = accounting.TotalAmount.FormatIndianCurrency(),
                ["👤 " + (type == NotifyType.Deleted ? "Deleted By" : "By")] = accounting.LastModifiedByUserName ?? accounting.CreatedByName,
                ["📅 Date"] = accounting.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt")
            },
            Remarks = accounting.Remarks
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task AccountingMail(int accountingId, NotifyType type, (MemoryStream, string)? previousInvoice = null)
    {
        var accounting = await CommonData.LoadTableDataById<AccountingOverviewModel>(ViewNames.AccountingOverview, accountingId);

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Accounting",
            TransactionNo = accounting.TransactionNo,
            Action = type,
            LocationName = accounting.VoucherName,
            Details = new Dictionary<string, string>
            {
                ["Transaction Number"] = accounting.TransactionNo,
                ["Voucher"] = accounting.VoucherName,
                ["Transaction Date"] = accounting.TransactionDateTime.ToString("dd MMM yyyy, hh:mm tt"),
                ["Total Ledgers"] = accounting.TotalLedgers.ToString(),
                ["Debit Ledgers"] = accounting.TotalDebitLedgers.ToString(),
                ["Credit Ledgers"] = accounting.TotalCreditLedgers.ToString(),
                ["Total Debit"] = accounting.TotalDebitAmount.FormatIndianCurrency(),
                ["Total Credit"] = accounting.TotalCreditAmount.FormatIndianCurrency(),
                ["Total Amount"] = accounting.TotalAmount.FormatIndianCurrency(),
                [type == NotifyType.Deleted ? "Deleted By" : type == NotifyType.Updated ? "Updated By" : "Modified By"] = accounting.LastModifiedByUserName ?? accounting.CreatedByName
            },
            Remarks = accounting.Remarks
        };

        // For update emails, include before and after invoices
        if (type == NotifyType.Updated && previousInvoice.HasValue)
        {
            var (afterStream, afterFileName) = await AccountingInvoiceExport.ExportInvoice(accountingId, InvoiceExportType.PDF);

            // Rename files to make it clear which is which
            var beforeFileName = $"BEFORE_{previousInvoice.Value.Item2}";
            var afterFileNameWithPrefix = $"AFTER_{afterFileName}";

            emailData.BeforeAttachment = (previousInvoice.Value.Item1, beforeFileName);
            emailData.AfterAttachment = (afterStream, afterFileNameWithPrefix);
        }
        else
        {
            // For delete/recover, just attach the current invoice
            var (pdfStream, pdfFileName) = await AccountingInvoiceExport.ExportInvoice(accountingId, InvoiceExportType.PDF);
            emailData.Attachments = new Dictionary<MemoryStream, string> { { pdfStream, pdfFileName } };
        }

        await MailingUtil.SendTransactionEmail(emailData);
    }
}
