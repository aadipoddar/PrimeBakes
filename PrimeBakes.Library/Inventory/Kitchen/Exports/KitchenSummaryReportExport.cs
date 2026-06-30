using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenSummaryReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenSummaryModel> kitchenSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenSummaryModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(KitchenSummaryModel.KitchenIssue)] = new() { DisplayName = "Kitchen Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenSummaryModel.KitchenProduction)] = new() { DisplayName = "Kitchen Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },

			[nameof(KitchenSummaryModel.TransactionCount)] = new() { DisplayName = "Transactions", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenSummaryModel.UnitsProduced)] = new() { DisplayName = "Units Produced", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },

			[nameof(KitchenSummaryModel.ContributionPercent)] = new() { DisplayName = "Contribution %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenSummaryModel.NetProduction)] = new() { DisplayName = "Net Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenSummaryModel.AverageProductionValue)] = new() { DisplayName = "Avg Production Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenSummaryModel.KitchenProductionPercent)] = new() { DisplayName = "Kitchen Production %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false }

		};

		List<string> columnOrder = showAllColumns
			?
			[
				nameof(KitchenSummaryModel.KitchenName),
				nameof(KitchenSummaryModel.KitchenIssue),
				nameof(KitchenSummaryModel.KitchenProduction),
				nameof(KitchenSummaryModel.TransactionCount),
				nameof(KitchenSummaryModel.UnitsProduced),
				nameof(KitchenSummaryModel.ContributionPercent),
				nameof(KitchenSummaryModel.NetProduction),
				nameof(KitchenSummaryModel.AverageProductionValue),
				nameof(KitchenSummaryModel.KitchenProductionPercent)
			]
			:
			[
				nameof(KitchenSummaryModel.KitchenName),
				nameof(KitchenSummaryModel.KitchenIssue),
				nameof(KitchenSummaryModel.KitchenProduction),
				nameof(KitchenSummaryModel.KitchenProductionPercent)
			];

		string fileName = "KITCHEN_SUMMARY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Company"] = company?.Name
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenSummaryData,
				"KITCHEN SUMMARY REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true,
				metadata);

			return (stream, fileName + ".pdf");
		}

		var excelStream = await ExcelReportExportUtil.ExportToExcel(
			kitchenSummaryData,
			"KITCHEN SUMMARY REPORT",
			"Kitchen Summary",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
