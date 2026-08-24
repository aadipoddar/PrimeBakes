using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Summary;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Summary;

public static class OrderItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<OrderItemMonthlySummaryModel> monthlySummaryData,
		List<string> monthHeaders,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OrderItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.FulfilledQuantity)] = new() { DisplayName = "Fulfilled", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.PendingQuantity)] = new() { DisplayName = "Pending", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.FulfilmentPercent)] = new() { DisplayName = "Fulfilment %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.PendingPercent)] = new() { DisplayName = "Pending %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.OrderCount)] = new() { DisplayName = "Orders", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.FulfilledOrderCount)] = new() { DisplayName = "Fulfilled Orders", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.LocationCount)] = new() { DisplayName = "Outlets", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.AveragePerOrder)] = new() { DisplayName = "Avg / Order", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(OrderItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(OrderItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemMonthlySummaryModel.FirstOrderDateTime)] = new() { DisplayName = "First Order", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.LastOrderDateTime)] = new() { DisplayName = "Last Order", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemMonthlySummaryModel.MonthsSinceLastOrder)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(OrderItemMonthlySummaryModel.ItemName),
			nameof(OrderItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
			columnOrder.Insert(1, nameof(OrderItemMonthlySummaryModel.ItemCode));

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(OrderItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(OrderItemMonthlySummaryModel.FulfilledQuantity),
				nameof(OrderItemMonthlySummaryModel.PendingQuantity),
				nameof(OrderItemMonthlySummaryModel.FulfilmentPercent),
				nameof(OrderItemMonthlySummaryModel.PendingPercent),
				nameof(OrderItemMonthlySummaryModel.OrderCount),
				nameof(OrderItemMonthlySummaryModel.FulfilledOrderCount),
				nameof(OrderItemMonthlySummaryModel.LocationCount),
				nameof(OrderItemMonthlySummaryModel.Rank),
				nameof(OrderItemMonthlySummaryModel.ContributionPercent),
				nameof(OrderItemMonthlySummaryModel.ActiveMonths),
				nameof(OrderItemMonthlySummaryModel.AveragePerMonth),
				nameof(OrderItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(OrderItemMonthlySummaryModel.AveragePerOrder),
				nameof(OrderItemMonthlySummaryModel.PeakMonthName),
				nameof(OrderItemMonthlySummaryModel.PeakMonthValue),
				nameof(OrderItemMonthlySummaryModel.LowestMonthName),
				nameof(OrderItemMonthlySummaryModel.LowestMonthValue),
				nameof(OrderItemMonthlySummaryModel.Quarter1),
				nameof(OrderItemMonthlySummaryModel.Quarter2),
				nameof(OrderItemMonthlySummaryModel.Quarter3),
				nameof(OrderItemMonthlySummaryModel.Quarter4),
				nameof(OrderItemMonthlySummaryModel.FirstHalf),
				nameof(OrderItemMonthlySummaryModel.SecondHalf),
				nameof(OrderItemMonthlySummaryModel.GrowthPercent),
				nameof(OrderItemMonthlySummaryModel.RecentTrendPercent),
				nameof(OrderItemMonthlySummaryModel.ConsistencyPercent),
				nameof(OrderItemMonthlySummaryModel.FirstOrderDateTime),
				nameof(OrderItemMonthlySummaryModel.LastOrderDateTime),
				nameof(OrderItemMonthlySummaryModel.MonthsSinceLastOrder)
			]);

		string fileName = "ORDER_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Item"] = product?.Name ?? null,
			["Category"] = productCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Location"] = location?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"ORDER ITEM MONTHLY REPORT",
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
			"ORDER ITEM MONTHLY REPORT",
			"Order Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
