using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Inventory.Summary;

public static class KitchenProductionItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<KitchenProductionItemMonthlySummaryModel> monthlySummaryData,
		List<string> monthHeaders,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		bool showAmount = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		KitchenModel kitchen = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenProductionItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.AverageRate)] = new() { DisplayName = "Avg Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.MinimumRate)] = new() { DisplayName = "Min Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.MaximumRate)] = new() { DisplayName = "Max Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.LastRate)] = new() { DisplayName = "Last Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.RateVariancePercent)] = new() { DisplayName = "Rate Swing %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.ReturnQuantity)] = new() { DisplayName = "Return Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.ReturnAmount)] = new() { DisplayName = "Return Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.ReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.TransactionCount)] = new() { DisplayName = "Productions", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.KitchenCount)] = new() { DisplayName = "Kitchens", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.AveragePerTransaction)] = new() { DisplayName = "Avg / Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenProductionItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenProductionItemMonthlySummaryModel.FirstProductionDateTime)] = new() { DisplayName = "First Production", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.LastProductionDateTime)] = new() { DisplayName = "Last Production", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemMonthlySummaryModel.MonthsSinceLastProduction)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(KitchenProductionItemMonthlySummaryModel.ItemName),
			nameof(KitchenProductionItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
			columnOrder.Insert(1, nameof(KitchenProductionItemMonthlySummaryModel.ItemCode));

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(KitchenProductionItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(KitchenProductionItemMonthlySummaryModel.TotalQuantity),
				nameof(KitchenProductionItemMonthlySummaryModel.TotalAmount),
				nameof(KitchenProductionItemMonthlySummaryModel.AverageRate),
				nameof(KitchenProductionItemMonthlySummaryModel.MinimumRate),
				nameof(KitchenProductionItemMonthlySummaryModel.MaximumRate),
				nameof(KitchenProductionItemMonthlySummaryModel.LastRate),
				nameof(KitchenProductionItemMonthlySummaryModel.RateVariancePercent),
				nameof(KitchenProductionItemMonthlySummaryModel.ReturnQuantity),
				nameof(KitchenProductionItemMonthlySummaryModel.ReturnAmount),
				nameof(KitchenProductionItemMonthlySummaryModel.ReturnPercent),
				nameof(KitchenProductionItemMonthlySummaryModel.TransactionCount),
				nameof(KitchenProductionItemMonthlySummaryModel.KitchenCount),
				nameof(KitchenProductionItemMonthlySummaryModel.Rank),
				nameof(KitchenProductionItemMonthlySummaryModel.ContributionPercent),
				nameof(KitchenProductionItemMonthlySummaryModel.ActiveMonths),
				nameof(KitchenProductionItemMonthlySummaryModel.AveragePerMonth),
				nameof(KitchenProductionItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(KitchenProductionItemMonthlySummaryModel.AveragePerTransaction),
				nameof(KitchenProductionItemMonthlySummaryModel.PeakMonthName),
				nameof(KitchenProductionItemMonthlySummaryModel.PeakMonthValue),
				nameof(KitchenProductionItemMonthlySummaryModel.LowestMonthName),
				nameof(KitchenProductionItemMonthlySummaryModel.LowestMonthValue),
				nameof(KitchenProductionItemMonthlySummaryModel.Quarter1),
				nameof(KitchenProductionItemMonthlySummaryModel.Quarter2),
				nameof(KitchenProductionItemMonthlySummaryModel.Quarter3),
				nameof(KitchenProductionItemMonthlySummaryModel.Quarter4),
				nameof(KitchenProductionItemMonthlySummaryModel.FirstHalf),
				nameof(KitchenProductionItemMonthlySummaryModel.SecondHalf),
				nameof(KitchenProductionItemMonthlySummaryModel.GrowthPercent),
				nameof(KitchenProductionItemMonthlySummaryModel.RecentTrendPercent),
				nameof(KitchenProductionItemMonthlySummaryModel.ConsistencyPercent),
				nameof(KitchenProductionItemMonthlySummaryModel.FirstProductionDateTime),
				nameof(KitchenProductionItemMonthlySummaryModel.LastProductionDateTime),
				nameof(KitchenProductionItemMonthlySummaryModel.MonthsSinceLastProduction)
			]);

		string fileName = "KITCHEN_PRODUCTION_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Measure"] = showAmount ? "Amount" : "Quantity",
			["Item"] = product?.Name ?? null,
			["Category"] = productCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Kitchen"] = kitchen?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"KITCHEN PRODUCTION ITEM MONTHLY REPORT",
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
			monthlySummaryData,
			"KITCHEN PRODUCTION ITEM MONTHLY REPORT",
			"Kitchen Production Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
