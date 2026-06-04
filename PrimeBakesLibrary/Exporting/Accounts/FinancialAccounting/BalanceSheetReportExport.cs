using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class BalanceSheetReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportAssetsReport(
		IEnumerable<TrialBalanceModel> assetsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TrialBalanceModel.LedgerName)] = new() { DisplayName = "Ledger Name", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TrialBalanceModel.GroupName)] = new() { DisplayName = "Group", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TrialBalanceModel.OpeningBalance)] = new() { DisplayName = "Opening Balance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TrialBalanceModel.Debit)] = new() { DisplayName = "Period Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TrialBalanceModel.ClosingBalance)] = new() { DisplayName = "Closing Balance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(TrialBalanceModel.LedgerName),
				nameof(TrialBalanceModel.GroupName),
				nameof(TrialBalanceModel.OpeningBalance),
				nameof(TrialBalanceModel.Debit),
				nameof(TrialBalanceModel.ClosingBalance)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(TrialBalanceModel.LedgerName),
				nameof(TrialBalanceModel.ClosingBalance)
			];
		}

		string fileName = $"ASSETS_STATEMENT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				assetsData,
				"ASSETS STATEMENT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				new() { ["Company Name"] = company?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				assetsData,
				"ASSETS STATEMENT",
				"Assets",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company Name"] = company?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportLiabilitiesReport(
		IEnumerable<TrialBalanceModel> liabilitiesData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TrialBalanceModel.LedgerName)] = new() { DisplayName = "Ledger Name", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TrialBalanceModel.GroupName)] = new() { DisplayName = "Group", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TrialBalanceModel.OpeningBalance)] = new() { DisplayName = "Opening Balance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TrialBalanceModel.Credit)] = new() { DisplayName = "Period Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(TrialBalanceModel.ClosingBalance)] = new() { DisplayName = "Closing Balance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(TrialBalanceModel.LedgerName),
				nameof(TrialBalanceModel.GroupName),
				nameof(TrialBalanceModel.OpeningBalance),
				nameof(TrialBalanceModel.Credit),
				nameof(TrialBalanceModel.ClosingBalance)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(TrialBalanceModel.LedgerName),
				nameof(TrialBalanceModel.ClosingBalance)
			];
		}

		string fileName = $"LIABILITIES_STATEMENT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				liabilitiesData,
				"LIABILITIES STATEMENT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				new() { ["Company Name"] = company?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				liabilitiesData,
				"LIABILITIES STATEMENT",
				"Liabilities",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company Name"] = company?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
