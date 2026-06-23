using PrimeBakes.Library.Accounts.FinancialAccounting.Models;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class FinancialAccountingInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerOverviewModel>(AccountNames.FinancialAccountingLedgerOverview, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId) ?? throw new InvalidOperationException("Company information is missing.");

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
			PaymentModes = null
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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"ACCOUNTING_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
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
			var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
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
