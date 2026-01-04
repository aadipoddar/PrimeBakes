using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Pdf.Graphics;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingInvoicePDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId) ?? throw new InvalidOperationException("Company information is missing.");
        var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, transaction.VoucherId);
        var allLedgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

        var cartItems = transactionDetails.Select(detail =>
        {
            var ledger = allLedgers.FirstOrDefault(l => l.Id == detail.LedgerId);
            return new AccountingItemCartModel
            {
                LedgerId = detail.LedgerId,
                LedgerName = ledger?.Name ?? $"Ledger #{detail.LedgerId}",
                ReferenceNo = detail.ReferenceNo,
                ReferenceType = detail.ReferenceType,
                Debit = detail.Debit,
                Credit = detail.Credit,
                Remarks = detail.Remarks
            };
        }).ToList();

        decimal totalDebit = cartItems.Sum(i => i.Debit ?? 0);
        decimal totalCredit = cartItems.Sum(i => i.Credit ?? 0);
        decimal difference = totalDebit - totalCredit;

        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            Company = company,
            BillTo = null,
            InvoiceType = voucher.Name.ToUpper(),
            TransactionNo = transaction.TransactionNo,
            TransactionDateTime = transaction.TransactionDateTime,
            ReferenceTransactionNo = transaction.ReferenceNo,
            TotalAmount = Math.Max(transaction.TotalDebitAmount, transaction.TotalCreditAmount),
            Remarks = transaction.Remarks,
            Status = transaction.Status,
            PaymentModes = null
        };

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 25, PdfTextAlignment.Center),
            new(nameof(AccountingItemCartModel.LedgerName), "Ledger", 0, PdfTextAlignment.Left),
            new(nameof(AccountingItemCartModel.ReferenceNo), "Ref No", 80, PdfTextAlignment.Left),
            new(nameof(AccountingItemCartModel.Debit), "Dr", 70, PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(AccountingItemCartModel.Credit), "Cr", 70, PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(AccountingItemCartModel.Remarks), "Remarks", 100, PdfTextAlignment.Left)
        };

        var summaryFields = new Dictionary<string, string>
        {
            ["Total Debit"] = totalDebit.FormatIndianCurrency(),
            ["Total Credit"] = totalCredit.FormatIndianCurrency(),
            ["Difference"] = difference.FormatIndianCurrency()
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            cartItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"ACCOUNTING_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}