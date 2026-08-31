using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Exports.Inventory.Summary;

public static class KitchenIssueItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<KitchenIssueItemMonthlySummaryModel> monthlySummaryData,
		List<string> monthHeaders,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		bool showAmount = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		CompanyModel company = null,
		KitchenModel kitchen = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenIssueItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.AverageRate)] = new() { DisplayName = "Avg Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.MinimumRate)] = new() { DisplayName = "Min Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.MaximumRate)] = new() { DisplayName = "Max Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.LastRate)] = new() { DisplayName = "Last Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.RateVariancePercent)] = new() { DisplayName = "Rate Swing %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.ReturnQuantity)] = new() { DisplayName = "Return Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.ReturnAmount)] = new() { DisplayName = "Return Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.ReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.TransactionCount)] = new() { DisplayName = "Issues", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.KitchenCount)] = new() { DisplayName = "Kitchens", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.AveragePerTransaction)] = new() { DisplayName = "Avg / Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(KitchenIssueItemMonthlySummaryModel.FirstIssueDateTime)] = new() { DisplayName = "First Issue", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.LastIssueDateTime)] = new() { DisplayName = "Last Issue", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemMonthlySummaryModel.MonthsSinceLastIssue)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(KitchenIssueItemMonthlySummaryModel.ItemName),
			nameof(KitchenIssueItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
		{
			columnOrder.Insert(1, nameof(KitchenIssueItemMonthlySummaryModel.ItemCode));
			columnOrder.Add(nameof(KitchenIssueItemMonthlySummaryModel.UnitOfMeasurement));
		}

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(KitchenIssueItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(KitchenIssueItemMonthlySummaryModel.TotalQuantity),
				nameof(KitchenIssueItemMonthlySummaryModel.TotalAmount),
				nameof(KitchenIssueItemMonthlySummaryModel.AverageRate),
				nameof(KitchenIssueItemMonthlySummaryModel.MinimumRate),
				nameof(KitchenIssueItemMonthlySummaryModel.MaximumRate),
				nameof(KitchenIssueItemMonthlySummaryModel.LastRate),
				nameof(KitchenIssueItemMonthlySummaryModel.RateVariancePercent),
				nameof(KitchenIssueItemMonthlySummaryModel.ReturnQuantity),
				nameof(KitchenIssueItemMonthlySummaryModel.ReturnAmount),
				nameof(KitchenIssueItemMonthlySummaryModel.ReturnPercent),
				nameof(KitchenIssueItemMonthlySummaryModel.TransactionCount),
				nameof(KitchenIssueItemMonthlySummaryModel.KitchenCount),
				nameof(KitchenIssueItemMonthlySummaryModel.Rank),
				nameof(KitchenIssueItemMonthlySummaryModel.ContributionPercent),
				nameof(KitchenIssueItemMonthlySummaryModel.ActiveMonths),
				nameof(KitchenIssueItemMonthlySummaryModel.AveragePerMonth),
				nameof(KitchenIssueItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(KitchenIssueItemMonthlySummaryModel.AveragePerTransaction),
				nameof(KitchenIssueItemMonthlySummaryModel.PeakMonthName),
				nameof(KitchenIssueItemMonthlySummaryModel.PeakMonthValue),
				nameof(KitchenIssueItemMonthlySummaryModel.LowestMonthName),
				nameof(KitchenIssueItemMonthlySummaryModel.LowestMonthValue),
				nameof(KitchenIssueItemMonthlySummaryModel.Quarter1),
				nameof(KitchenIssueItemMonthlySummaryModel.Quarter2),
				nameof(KitchenIssueItemMonthlySummaryModel.Quarter3),
				nameof(KitchenIssueItemMonthlySummaryModel.Quarter4),
				nameof(KitchenIssueItemMonthlySummaryModel.FirstHalf),
				nameof(KitchenIssueItemMonthlySummaryModel.SecondHalf),
				nameof(KitchenIssueItemMonthlySummaryModel.GrowthPercent),
				nameof(KitchenIssueItemMonthlySummaryModel.RecentTrendPercent),
				nameof(KitchenIssueItemMonthlySummaryModel.ConsistencyPercent),
				nameof(KitchenIssueItemMonthlySummaryModel.FirstIssueDateTime),
				nameof(KitchenIssueItemMonthlySummaryModel.LastIssueDateTime),
				nameof(KitchenIssueItemMonthlySummaryModel.MonthsSinceLastIssue)
			]);

		string fileName = "KITCHEN_ISSUE_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Measure"] = showAmount ? "Amount" : "Quantity",
			["Item"] = rawMaterial?.Name ?? null,
			["Category"] = rawMaterialCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Kitchen"] = kitchen?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"KITCHEN ISSUE ITEM MONTHLY REPORT",
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
			"KITCHEN ISSUE ITEM MONTHLY REPORT",
			"Kitchen Issue Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
