using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Exports.Inventory.Summary;

public static class PurchaseItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<PurchaseItemMonthlySummaryModel> monthlySummaryData,
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
		LedgerModel party = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.AverageRate)] = new() { DisplayName = "Avg Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.MinimumRate)] = new() { DisplayName = "Min Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.MaximumRate)] = new() { DisplayName = "Max Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.LastRate)] = new() { DisplayName = "Last Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.RateVariancePercent)] = new() { DisplayName = "Rate Swing %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.DiscountAmount)] = new() { DisplayName = "Discount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.TaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.ReturnQuantity)] = new() { DisplayName = "Return Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.ReturnAmount)] = new() { DisplayName = "Return Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.ReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.TransactionCount)] = new() { DisplayName = "Purchases", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.PartyCount)] = new() { DisplayName = "Suppliers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.AveragePerTransaction)] = new() { DisplayName = "Avg / Purchase", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseItemMonthlySummaryModel.FirstPurchaseDateTime)] = new() { DisplayName = "First Purchase", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.LastPurchaseDateTime)] = new() { DisplayName = "Last Purchase", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemMonthlySummaryModel.MonthsSinceLastPurchase)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(PurchaseItemMonthlySummaryModel.ItemName),
			nameof(PurchaseItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
		{
			columnOrder.Insert(1, nameof(PurchaseItemMonthlySummaryModel.ItemCode));
			columnOrder.Add(nameof(PurchaseItemMonthlySummaryModel.UnitOfMeasurement));
		}

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(PurchaseItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(PurchaseItemMonthlySummaryModel.TotalQuantity),
				nameof(PurchaseItemMonthlySummaryModel.TotalAmount),
				nameof(PurchaseItemMonthlySummaryModel.AverageRate),
				nameof(PurchaseItemMonthlySummaryModel.MinimumRate),
				nameof(PurchaseItemMonthlySummaryModel.MaximumRate),
				nameof(PurchaseItemMonthlySummaryModel.LastRate),
				nameof(PurchaseItemMonthlySummaryModel.RateVariancePercent),
				nameof(PurchaseItemMonthlySummaryModel.DiscountAmount),
				nameof(PurchaseItemMonthlySummaryModel.TaxAmount),
				nameof(PurchaseItemMonthlySummaryModel.ReturnQuantity),
				nameof(PurchaseItemMonthlySummaryModel.ReturnAmount),
				nameof(PurchaseItemMonthlySummaryModel.ReturnPercent),
				nameof(PurchaseItemMonthlySummaryModel.TransactionCount),
				nameof(PurchaseItemMonthlySummaryModel.PartyCount),
				nameof(PurchaseItemMonthlySummaryModel.Rank),
				nameof(PurchaseItemMonthlySummaryModel.ContributionPercent),
				nameof(PurchaseItemMonthlySummaryModel.ActiveMonths),
				nameof(PurchaseItemMonthlySummaryModel.AveragePerMonth),
				nameof(PurchaseItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(PurchaseItemMonthlySummaryModel.AveragePerTransaction),
				nameof(PurchaseItemMonthlySummaryModel.PeakMonthName),
				nameof(PurchaseItemMonthlySummaryModel.PeakMonthValue),
				nameof(PurchaseItemMonthlySummaryModel.LowestMonthName),
				nameof(PurchaseItemMonthlySummaryModel.LowestMonthValue),
				nameof(PurchaseItemMonthlySummaryModel.Quarter1),
				nameof(PurchaseItemMonthlySummaryModel.Quarter2),
				nameof(PurchaseItemMonthlySummaryModel.Quarter3),
				nameof(PurchaseItemMonthlySummaryModel.Quarter4),
				nameof(PurchaseItemMonthlySummaryModel.FirstHalf),
				nameof(PurchaseItemMonthlySummaryModel.SecondHalf),
				nameof(PurchaseItemMonthlySummaryModel.GrowthPercent),
				nameof(PurchaseItemMonthlySummaryModel.RecentTrendPercent),
				nameof(PurchaseItemMonthlySummaryModel.ConsistencyPercent),
				nameof(PurchaseItemMonthlySummaryModel.FirstPurchaseDateTime),
				nameof(PurchaseItemMonthlySummaryModel.LastPurchaseDateTime),
				nameof(PurchaseItemMonthlySummaryModel.MonthsSinceLastPurchase)
			]);

		string fileName = "PURCHASE_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Measure"] = showAmount ? "Amount" : "Quantity",
			["Item"] = rawMaterial?.Name ?? null,
			["Category"] = rawMaterialCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Party"] = party?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"PURCHASE ITEM MONTHLY REPORT",
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
			"PURCHASE ITEM MONTHLY REPORT",
			"Purchase Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
