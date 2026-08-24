using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Summary;

namespace PrimeBakes.Exports.Store.Summary;

public static class SaleItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<SaleItemMonthlySummaryModel> monthlySummaryData,
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
		LocationModel location = null,
		LedgerModel party = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SaleItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.AverageRate)] = new() { DisplayName = "Avg Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.DiscountAmount)] = new() { DisplayName = "Discount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.TaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.ReturnQuantity)] = new() { DisplayName = "Return Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.ReturnAmount)] = new() { DisplayName = "Return Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.ReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.TransactionCount)] = new() { DisplayName = "Bills", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.LocationCount)] = new() { DisplayName = "Outlets", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.AveragePerTransaction)] = new() { DisplayName = "Avg / Bill", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(SaleItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(SaleItemMonthlySummaryModel.FirstSaleDateTime)] = new() { DisplayName = "First Sale", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.LastSaleDateTime)] = new() { DisplayName = "Last Sale", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemMonthlySummaryModel.MonthsSinceLastSale)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(SaleItemMonthlySummaryModel.ItemName),
			nameof(SaleItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
			columnOrder.Insert(1, nameof(SaleItemMonthlySummaryModel.ItemCode));

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(SaleItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(SaleItemMonthlySummaryModel.TotalQuantity),
				nameof(SaleItemMonthlySummaryModel.TotalAmount),
				nameof(SaleItemMonthlySummaryModel.AverageRate),
				nameof(SaleItemMonthlySummaryModel.DiscountAmount),
				nameof(SaleItemMonthlySummaryModel.TaxAmount),
				nameof(SaleItemMonthlySummaryModel.ReturnQuantity),
				nameof(SaleItemMonthlySummaryModel.ReturnAmount),
				nameof(SaleItemMonthlySummaryModel.ReturnPercent),
				nameof(SaleItemMonthlySummaryModel.TransactionCount),
				nameof(SaleItemMonthlySummaryModel.LocationCount),
				nameof(SaleItemMonthlySummaryModel.Rank),
				nameof(SaleItemMonthlySummaryModel.ContributionPercent),
				nameof(SaleItemMonthlySummaryModel.ActiveMonths),
				nameof(SaleItemMonthlySummaryModel.AveragePerMonth),
				nameof(SaleItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(SaleItemMonthlySummaryModel.AveragePerTransaction),
				nameof(SaleItemMonthlySummaryModel.PeakMonthName),
				nameof(SaleItemMonthlySummaryModel.PeakMonthValue),
				nameof(SaleItemMonthlySummaryModel.LowestMonthName),
				nameof(SaleItemMonthlySummaryModel.LowestMonthValue),
				nameof(SaleItemMonthlySummaryModel.Quarter1),
				nameof(SaleItemMonthlySummaryModel.Quarter2),
				nameof(SaleItemMonthlySummaryModel.Quarter3),
				nameof(SaleItemMonthlySummaryModel.Quarter4),
				nameof(SaleItemMonthlySummaryModel.FirstHalf),
				nameof(SaleItemMonthlySummaryModel.SecondHalf),
				nameof(SaleItemMonthlySummaryModel.GrowthPercent),
				nameof(SaleItemMonthlySummaryModel.RecentTrendPercent),
				nameof(SaleItemMonthlySummaryModel.ConsistencyPercent),
				nameof(SaleItemMonthlySummaryModel.FirstSaleDateTime),
				nameof(SaleItemMonthlySummaryModel.LastSaleDateTime),
				nameof(SaleItemMonthlySummaryModel.MonthsSinceLastSale)
			]);

		string fileName = "SALE_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Measure"] = showAmount ? "Amount" : "Quantity",
			["Item"] = product?.Name ?? null,
			["Category"] = productCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Location"] = location?.Name ?? null,
			["Party"] = party?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"SALE ITEM MONTHLY REPORT",
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
			"SALE ITEM MONTHLY REPORT",
			"Sale Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
