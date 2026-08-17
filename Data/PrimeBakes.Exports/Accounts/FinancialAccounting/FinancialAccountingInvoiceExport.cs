using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Exports.Accounts.FinancialAccounting;

public static class FinancialAccountingInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(FinancialAccountingInvoiceBundle bundle, InvoiceExportType exportType)
	{
		var (transaction, transactionDetails, company, currentDateTime) = bundle;
		var invoiceData = new InvoiceData
		{
			Company = company,
			BillTo = null,
			InvoiceType = transaction.VoucherName.ToUpperInvariant(),
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = transaction.ReferenceNo,
			TotalAmount = Math.Max(transaction.TotalDebitAmount, transaction.TotalCreditAmount),
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = null,
			CurrentDateTime = currentDateTime
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(FinancialAccountingLedgerOverviewModel.LedgerName), "Ledger", exportType, CellAlignment.Left, 0, 35),
			new(nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceNo), "Ref No", exportType, CellAlignment.Left, 80, 15),
			new(nameof(FinancialAccountingLedgerOverviewModel.Debit), "Dr", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
			new(nameof(FinancialAccountingLedgerOverviewModel.Credit), "Cr", exportType, CellAlignment.Right, 70, 15, "#,##0.00"),
			new(nameof(FinancialAccountingLedgerOverviewModel.LedgerRemarks), "Remarks", exportType, CellAlignment.Left, 100, 25)
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Total Debit"] = transaction.TotalDebitAmount.FormatIndianCurrency(),
			["Total Credit"] = transaction.TotalCreditAmount.FormatIndianCurrency(),
			["Difference"] = (transaction.TotalDebitAmount - transaction.TotalCreditAmount).FormatIndianCurrency()
		};

		string fileName = $"ACCOUNTING_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = PDFInvoiceExportUtil.ExportInvoiceToPdf(
				invoiceData,
				transactionDetails,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = ExcelInvoiceExportUtil.ExportInvoiceToExcel(
				invoiceData,
				transactionDetails,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
