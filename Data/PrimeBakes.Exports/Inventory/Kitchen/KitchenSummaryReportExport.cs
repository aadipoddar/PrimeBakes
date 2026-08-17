using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Exports.Inventory.Kitchen;

public static class KitchenSummaryReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<KitchenSummaryModel> kitchenSummaryData,
		DateTime currentDateTime,
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
			[nameof(KitchenSummaryModel.KitchenIssueReturn)] = new() { DisplayName = "Kitchen Issue Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenSummaryModel.NetKitchenIssue)] = new() { DisplayName = "Net Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenSummaryModel.KitchenProduction)] = new() { DisplayName = "Kitchen Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenSummaryModel.KitchenProductionReturn)] = new() { DisplayName = "Kitchen Production Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenSummaryModel.NetKitchenProduction)] = new() { DisplayName = "Net Production Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

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
				nameof(KitchenSummaryModel.KitchenIssueReturn),
				nameof(KitchenSummaryModel.NetKitchenIssue),
				nameof(KitchenSummaryModel.KitchenProduction),
				nameof(KitchenSummaryModel.KitchenProductionReturn),
				nameof(KitchenSummaryModel.NetKitchenProduction),
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
				nameof(KitchenSummaryModel.KitchenIssueReturn),
				nameof(KitchenSummaryModel.KitchenProduction),
				nameof(KitchenSummaryModel.KitchenProductionReturn),
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
			var stream = PDFReportExportUtil.ExportToPdf(
				kitchenSummaryData,
				"KITCHEN SUMMARY REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true,
				metadata);

			return (stream, fileName + ".pdf");
		}

		var excelStream = ExcelReportExportUtil.ExportToExcel(
			kitchenSummaryData,
			"KITCHEN SUMMARY REPORT",
			"Kitchen Summary",
				currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
