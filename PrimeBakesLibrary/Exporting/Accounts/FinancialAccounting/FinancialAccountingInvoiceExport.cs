using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class FinancialAccountingInvoiceExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
    {
        var transaction = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<FinancialAccountingDetailModel>(TableNames.FinancialAccountingDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId) ?? throw new InvalidOperationException("Company information is missing.");
        var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, transaction.VoucherId);
        var allLedgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

        var cartItems = transactionDetails.Select(detail =>
        {
            var ledger = allLedgers.FirstOrDefault(l => l.Id == detail.LedgerId);
            return new FinancialAccountingItemCartModel
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

        var invoiceData = new InvoiceData
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

        var columnSettings = new List<InvoiceColumnSetting>
        {
            new("#", "#", exportType, CellAlignment.Center, 25, 5),
            new(nameof(FinancialAccountingItemCartModel.LedgerName), "Ledger", exportType, CellAlignment.Left, 0, 35),
            new(nameof(FinancialAccountingItemCartModel.ReferenceNo), "Ref No", exportType, CellAlignment.Left, 80, 15),
            new(nameof(FinancialAccountingItemCartModel.Debit), "Dr", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
            new(nameof(FinancialAccountingItemCartModel.Credit), "Cr", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
            new(nameof(FinancialAccountingItemCartModel.Remarks), "Remarks", exportType, CellAlignment.Left, 100, 25)
        };

        var summaryFields = new Dictionary<string, string>
        {
            ["Total Debit"] = totalDebit.FormatIndianCurrency(),
            ["Total Credit"] = totalCredit.FormatIndianCurrency(),
            ["Difference"] = difference.FormatIndianCurrency()
        };

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"ACCOUNTING_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == InvoiceExportType.PDF)
        {
            var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
                invoiceData,
                cartItems,
                columnSettings,
                null,
                summaryFields
            );

            fileName += ".pdf";
            return (stream, fileName);
        }
        else
        {
            var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
                invoiceData,
                cartItems,
                columnSettings,
                null,
                summaryFields
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }
}
