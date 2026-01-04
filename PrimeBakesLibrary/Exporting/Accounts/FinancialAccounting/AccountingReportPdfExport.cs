using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingReportPdfExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<AccountingOverviewModel> accountingData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null,
		VoucherModel voucher = null)
	{
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(AccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(AccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },

			[nameof(AccountingOverviewModel.TransactionDateTime)] = new()
			{
				DisplayName = "Trans Date",
				Format = "dd-MMM-yyyy hh:mm tt",
				IncludeInTotal = false
			},

			[nameof(AccountingOverviewModel.TotalDebitLedgers)] = new()
			{
				DisplayName = "Debit Ledgers",
				Format = "#,##0",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(AccountingOverviewModel.TotalCreditLedgers)] = new()
			{
				DisplayName = "Credit Ledgers",
				Format = "#,##0",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(AccountingOverviewModel.TotalDebitAmount)] = new()
			{
				DisplayName = "Debit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(AccountingOverviewModel.TotalCreditAmount)] = new()
			{
				DisplayName = "Credit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(AccountingOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Amt",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			}
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(AccountingOverviewModel.TransactionNo),
				nameof(AccountingOverviewModel.TransactionDateTime),
				nameof(AccountingOverviewModel.ReferenceNo),
				nameof(AccountingOverviewModel.FinancialYear),
				nameof(AccountingOverviewModel.TotalDebitLedgers),
				nameof(AccountingOverviewModel.TotalCreditLedgers),
				nameof(AccountingOverviewModel.TotalDebitAmount),
				nameof(AccountingOverviewModel.TotalCreditAmount),
				nameof(AccountingOverviewModel.TotalAmount),
				nameof(AccountingOverviewModel.Remarks),
				nameof(AccountingOverviewModel.CreatedByName),
				nameof(AccountingOverviewModel.CreatedAt),
				nameof(AccountingOverviewModel.CreatedFromPlatform),
				nameof(AccountingOverviewModel.LastModifiedByUserName),
				nameof(AccountingOverviewModel.LastModifiedAt),
				nameof(AccountingOverviewModel.LastModifiedFromPlatform)
			];

			if (company is null)
				columnOrder.Insert(6, nameof(AccountingOverviewModel.CompanyName));

			if (voucher is null)
				columnOrder.Insert(7, nameof(AccountingOverviewModel.VoucherName));
		}

		else
			columnOrder =
			[
				nameof(AccountingOverviewModel.TransactionNo),
				nameof(AccountingOverviewModel.TransactionDateTime),
				nameof(AccountingOverviewModel.ReferenceNo),
				nameof(AccountingOverviewModel.TotalDebitAmount),
				nameof(AccountingOverviewModel.TotalCreditAmount),
				nameof(AccountingOverviewModel.TotalAmount)
			];

		var stream = await PDFReportExportUtil.ExportToPdf(
			accountingData,
			"FINANCIAL ACCOUNTING REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useBuiltInStyle: false,
			useLandscape: showAllColumns,
			new() { ["Company"] = company?.Name ?? null, ["Voucher"] = voucher?.Name ?? null }
		);

		string fileName = $"ACCOUNTING_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
		fileName += ".pdf";

		return (stream, fileName);
	}
}
