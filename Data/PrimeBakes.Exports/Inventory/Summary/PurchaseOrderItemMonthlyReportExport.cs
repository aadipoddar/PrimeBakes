using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Inventory.Summary;

namespace PrimeBakes.Exports.Inventory.Summary;

public static class PurchaseOrderItemMonthlyReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<PurchaseOrderItemMonthlySummaryModel> monthlySummaryData,
		List<string> monthHeaders,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		CompanyModel company = null,
		LedgerModel party = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseOrderItemMonthlySummaryModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.FulfilledQuantity)] = new() { DisplayName = "Received", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.PendingQuantity)] = new() { DisplayName = "Pending", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.FulfilmentPercent)] = new() { DisplayName = "Received %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.PendingPercent)] = new() { DisplayName = "Pending %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.OrderCount)] = new() { DisplayName = "Orders", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.FulfilledOrderCount)] = new() { DisplayName = "Received Orders", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.PartyCount)] = new() { DisplayName = "Suppliers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.Rank)] = new() { DisplayName = "Rank", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.ContributionPercent)] = new() { DisplayName = "Share %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.ActiveMonths)] = new() { DisplayName = "Active Months", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerMonth)] = new() { DisplayName = "Avg / Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerActiveMonth)] = new() { DisplayName = "Avg / Active Month", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerOrder)] = new() { DisplayName = "Avg / Order", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.PeakMonthName)] = new() { DisplayName = "Peak Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.PeakMonthValue)] = new() { DisplayName = "Peak Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.LowestMonthName)] = new() { DisplayName = "Lowest Month", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.LowestMonthValue)] = new() { DisplayName = "Lowest Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.Quarter1)] = new() { DisplayName = "Q1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.Quarter2)] = new() { DisplayName = "Q2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.Quarter3)] = new() { DisplayName = "Q3", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.Quarter4)] = new() { DisplayName = "Q4", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.FirstHalf)] = new() { DisplayName = "H1", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.SecondHalf)] = new() { DisplayName = "H2", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.GrowthPercent)] = new() { DisplayName = "H2 vs H1 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.RecentTrendPercent)] = new() { DisplayName = "Q4 vs Q3 %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseOrderItemMonthlySummaryModel.ConsistencyPercent)] = new() { DisplayName = "Consistency %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemMonthlySummaryModel.FirstOrderDateTime)] = new() { DisplayName = "First Order", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.LastOrderDateTime)] = new() { DisplayName = "Last Order", Format = "dd/MM/yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemMonthlySummaryModel.MonthsSinceLastOrder)] = new() { DisplayName = "Months Idle", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		for (var index = 0; index < 12; index++)
			columnSettings[$"Month{index + 1}"] = new() { DisplayName = monthHeaders[index], Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true };

		List<string> columnOrder =
		[
			nameof(PurchaseOrderItemMonthlySummaryModel.ItemName),
			nameof(PurchaseOrderItemMonthlySummaryModel.ItemCategoryName)
		];

		if (showAllColumns)
		{
			columnOrder.Insert(1, nameof(PurchaseOrderItemMonthlySummaryModel.ItemCode));
			columnOrder.Add(nameof(PurchaseOrderItemMonthlySummaryModel.UnitOfMeasurement));
		}

		for (var index = 0; index < 12; index++)
			columnOrder.Add($"Month{index + 1}");

		columnOrder.Add(nameof(PurchaseOrderItemMonthlySummaryModel.Total));

		if (showAllColumns)
			columnOrder.AddRange(
			[
				nameof(PurchaseOrderItemMonthlySummaryModel.FulfilledQuantity),
				nameof(PurchaseOrderItemMonthlySummaryModel.PendingQuantity),
				nameof(PurchaseOrderItemMonthlySummaryModel.FulfilmentPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.PendingPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.OrderCount),
				nameof(PurchaseOrderItemMonthlySummaryModel.FulfilledOrderCount),
				nameof(PurchaseOrderItemMonthlySummaryModel.PartyCount),
				nameof(PurchaseOrderItemMonthlySummaryModel.Rank),
				nameof(PurchaseOrderItemMonthlySummaryModel.ContributionPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.ActiveMonths),
				nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerMonth),
				nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerActiveMonth),
				nameof(PurchaseOrderItemMonthlySummaryModel.AveragePerOrder),
				nameof(PurchaseOrderItemMonthlySummaryModel.PeakMonthName),
				nameof(PurchaseOrderItemMonthlySummaryModel.PeakMonthValue),
				nameof(PurchaseOrderItemMonthlySummaryModel.LowestMonthName),
				nameof(PurchaseOrderItemMonthlySummaryModel.LowestMonthValue),
				nameof(PurchaseOrderItemMonthlySummaryModel.Quarter1),
				nameof(PurchaseOrderItemMonthlySummaryModel.Quarter2),
				nameof(PurchaseOrderItemMonthlySummaryModel.Quarter3),
				nameof(PurchaseOrderItemMonthlySummaryModel.Quarter4),
				nameof(PurchaseOrderItemMonthlySummaryModel.FirstHalf),
				nameof(PurchaseOrderItemMonthlySummaryModel.SecondHalf),
				nameof(PurchaseOrderItemMonthlySummaryModel.GrowthPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.RecentTrendPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.ConsistencyPercent),
				nameof(PurchaseOrderItemMonthlySummaryModel.FirstOrderDateTime),
				nameof(PurchaseOrderItemMonthlySummaryModel.LastOrderDateTime),
				nameof(PurchaseOrderItemMonthlySummaryModel.MonthsSinceLastOrder)
			]);

		string fileName = "PURCHASE_ORDER_ITEM_MONTHLY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Item"] = rawMaterial?.Name ?? null,
			["Category"] = rawMaterialCategory?.Name ?? null,
			["Company"] = company?.Name ?? null,
			["Party"] = party?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				monthlySummaryData,
				"PURCHASE ORDER ITEM MONTHLY REPORT",
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
			"PURCHASE ORDER ITEM MONTHLY REPORT",
			"PO Item Monthly",
			currentDateTime,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
