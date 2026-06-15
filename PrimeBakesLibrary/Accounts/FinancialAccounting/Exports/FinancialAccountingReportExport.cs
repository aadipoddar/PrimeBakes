using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Accounts.FinancialAccounting.Exports;

public static class FinancialAccountingReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<FinancialAccountingOverviewModel> accountingData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		CompanyModel company = null,
		VoucherModel voucher = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(FinancialAccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingOverviewModel.TotalDebitLedgers)] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingOverviewModel.TotalCreditLedgers)] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingOverviewModel.TotalDebitAmount)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingOverviewModel.TotalCreditAmount)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingOverviewModel.TotalAmount)] = new() { DisplayName = "Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(FinancialAccountingOverviewModel.TransactionNo),
				nameof(FinancialAccountingOverviewModel.TransactionDateTime),
				nameof(FinancialAccountingOverviewModel.CompanyName),
				nameof(FinancialAccountingOverviewModel.VoucherName),
				nameof(FinancialAccountingOverviewModel.ReferenceNo),
				nameof(FinancialAccountingOverviewModel.FinancialYear),
				nameof(FinancialAccountingOverviewModel.TotalDebitLedgers),
				nameof(FinancialAccountingOverviewModel.TotalCreditLedgers),
				nameof(FinancialAccountingOverviewModel.TotalDebitAmount),
				nameof(FinancialAccountingOverviewModel.TotalCreditAmount),
				nameof(FinancialAccountingOverviewModel.TotalAmount),
				nameof(FinancialAccountingOverviewModel.Remarks),
				nameof(FinancialAccountingOverviewModel.CreatedByName),
				nameof(FinancialAccountingOverviewModel.CreatedAt),
				nameof(FinancialAccountingOverviewModel.CreatedFromPlatform),
				nameof(FinancialAccountingOverviewModel.LastModifiedByUserName),
				nameof(FinancialAccountingOverviewModel.LastModifiedAt),
				nameof(FinancialAccountingOverviewModel.LastModifiedFromPlatform),
				nameof(FinancialAccountingOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(FinancialAccountingOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(FinancialAccountingOverviewModel.TransactionNo),
				nameof(FinancialAccountingOverviewModel.TransactionDateTime),
				nameof(FinancialAccountingOverviewModel.ReferenceNo),
				nameof(FinancialAccountingOverviewModel.TotalDebitAmount),
				nameof(FinancialAccountingOverviewModel.TotalCreditAmount),
				nameof(FinancialAccountingOverviewModel.TotalAmount),
				nameof(FinancialAccountingOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(FinancialAccountingOverviewModel.Status));
		}

		string fileName = $"ACCOUNTING_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
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

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				accountingData,
				"FINANCIAL ACCOUNTING REPORT",
				"Accounting Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Voucher"] = voucher?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportLedgerReport(
		IEnumerable<FinancialAccountingLedgerOverviewModel> ledgerData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = true,
		CompanyModel company = null,
		LedgerModel ledger = null,
		TrialBalanceModel trialBalance = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(FinancialAccountingLedgerOverviewModel.LedgerName)] = new() { DisplayName = "Ledger", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.LedgerCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.AccountTypeName)] = new() { DisplayName = "Account Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.GroupName)] = new() { DisplayName = "Group", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceType)] = new() { DisplayName = "Ref Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceNo)] = new() { DisplayName = "Ref No", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(FinancialAccountingLedgerOverviewModel.InstrumentNo)] = new() { DisplayName = "Instrument No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.InstrumentDate)] = new() { DisplayName = "Instrument Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.ClearingDate)] = new() { DisplayName = "Clearing Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.ReconciledStatus)] = new() { DisplayName = "Reconciled", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(FinancialAccountingLedgerOverviewModel.Debit)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingLedgerOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },

			[nameof(FinancialAccountingLedgerOverviewModel.LedgerRemarks)] = new() { DisplayName = "Ledger Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(FinancialAccountingLedgerOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialAccountingLedgerOverviewModel.TotalDebitLedgers)] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingLedgerOverviewModel.TotalCreditLedgers)] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingLedgerOverviewModel.TotalDebitAmount)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingLedgerOverviewModel.TotalCreditAmount)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(FinancialAccountingLedgerOverviewModel.TotalAmount)] = new() { DisplayName = "Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(FinancialAccountingLedgerOverviewModel.LedgerName),
				nameof(FinancialAccountingLedgerOverviewModel.AccountTypeName),
				nameof(FinancialAccountingLedgerOverviewModel.GroupName),
				nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceType),
				nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceNo),
				nameof(FinancialAccountingLedgerOverviewModel.InstrumentNo),
				nameof(FinancialAccountingLedgerOverviewModel.InstrumentDate),
				nameof(FinancialAccountingLedgerOverviewModel.ClearingDate),
				nameof(FinancialAccountingLedgerOverviewModel.ReconciledStatus),
				nameof(FinancialAccountingLedgerOverviewModel.Debit),
				nameof(FinancialAccountingLedgerOverviewModel.Credit),
				nameof(FinancialAccountingLedgerOverviewModel.LedgerRemarks),
				nameof(FinancialAccountingLedgerOverviewModel.TransactionNo),
				nameof(FinancialAccountingLedgerOverviewModel.TransactionDateTime),
				nameof(FinancialAccountingLedgerOverviewModel.CompanyName),
				nameof(FinancialAccountingLedgerOverviewModel.VoucherName),
				nameof(FinancialAccountingLedgerOverviewModel.ReferenceNo),
				nameof(FinancialAccountingLedgerOverviewModel.FinancialYear),
				nameof(FinancialAccountingLedgerOverviewModel.TotalDebitLedgers),
				nameof(FinancialAccountingLedgerOverviewModel.TotalCreditLedgers),
				nameof(FinancialAccountingLedgerOverviewModel.TotalDebitAmount),
				nameof(FinancialAccountingLedgerOverviewModel.TotalCreditAmount),
				nameof(FinancialAccountingLedgerOverviewModel.TotalAmount),
				nameof(FinancialAccountingLedgerOverviewModel.Remarks),
				nameof(FinancialAccountingLedgerOverviewModel.CreatedByName),
				nameof(FinancialAccountingLedgerOverviewModel.CreatedAt),
				nameof(FinancialAccountingLedgerOverviewModel.CreatedFromPlatform),
				nameof(FinancialAccountingLedgerOverviewModel.LastModifiedByUserName),
				nameof(FinancialAccountingLedgerOverviewModel.LastModifiedAt),
				nameof(FinancialAccountingLedgerOverviewModel.LastModifiedFromPlatform),
				nameof(FinancialAccountingLedgerOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(FinancialAccountingLedgerOverviewModel.MasterStatus));
		}
		else
		{
			columnOrder =
			[
				nameof(FinancialAccountingLedgerOverviewModel.LedgerName),
				nameof(FinancialAccountingLedgerOverviewModel.TransactionNo),
				nameof(FinancialAccountingLedgerOverviewModel.TransactionDateTime),
				nameof(FinancialAccountingLedgerOverviewModel.LedgerReferenceNo),
				nameof(FinancialAccountingLedgerOverviewModel.InstrumentNo),
				nameof(FinancialAccountingLedgerOverviewModel.InstrumentDate),
				nameof(FinancialAccountingLedgerOverviewModel.ClearingDate),
				nameof(FinancialAccountingLedgerOverviewModel.ReconciledStatus),
				nameof(FinancialAccountingLedgerOverviewModel.Debit),
				nameof(FinancialAccountingLedgerOverviewModel.Credit),
				nameof(FinancialAccountingLedgerOverviewModel.LedgerRemarks),
				nameof(FinancialAccountingLedgerOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(FinancialAccountingLedgerOverviewModel.MasterStatus));

			if (ledger is not null)
				columnOrder.Remove(nameof(FinancialAccountingLedgerOverviewModel.LedgerName));
		}

		Dictionary<string, string> customSummaryFields = null;
		if (trialBalance is not null)
			customSummaryFields = new Dictionary<string, string>
			{
				["Opening Balance"] = $"₹ {trialBalance.OpeningBalance:N2}",
				["Closing Balance"] = $"₹ {trialBalance.ClosingBalance:N2}"
			};

		string fileName = $"LEDGER_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				ledgerData,
				"FINANCIAL LEDGER REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				new() { ["Company"] = company?.Name ?? null, ["Ledger"] = ledger?.Name ?? null },
				customSummaryFields: customSummaryFields
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				ledgerData,
				"FINANCIAL LEDGER REPORT",
				"Ledger Report",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Ledger"] = ledger?.Name ?? null },
				customSummaryFields
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
