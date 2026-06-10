using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Inventory.Stock.Exports;

public static class RawMaterialStockReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportSummaryReport(
		IEnumerable<RawMaterialStockSummaryModel> stockData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },

			// Stock quantities
			[nameof(RawMaterialStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.InStock)] = new() { DisplayName = "Stock In", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RawMaterialStockSummaryModel.OutStock)] = new() { DisplayName = "Stock Out", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RawMaterialStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Net Movement", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.PurchaseReturnStock)] = new() { DisplayName = "Purchase Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.KitchenIssueStock)] = new() { DisplayName = "Kitchen Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.KitchenProductionStock)] = new() { DisplayName = "Kitchen Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.SaleReturnStock)] = new() { DisplayName = "Sale Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.StockTransferStock)] = new() { DisplayName = "Stock Transfer", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.BillStock)] = new() { DisplayName = "Bill", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.AdjustmentStock)] = new() { DisplayName = "Adjustment", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			// Rate & valuation
			[nameof(RawMaterialStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.ClosingValueByRate)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.AverageInRate)] = new() { DisplayName = "Avg In Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.ClosingValueByAverageInRate)] = new() { DisplayName = "Value at Avg In", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.AverageOutRate)] = new() { DisplayName = "Avg Out Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.ClosingValueByAverageOutRate)] = new() { DisplayName = "Value at Avg Out", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.RateVariance)] = new() { DisplayName = "Rate Variance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			// Movement values (₹)
			[nameof(RawMaterialStockSummaryModel.OpeningValue)] = new() { DisplayName = "Opening Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.TotalInValue)] = new() { DisplayName = "Total In Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.TotalOutValue)] = new() { DisplayName = "Total Out Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.PurchaseValue)] = new() { DisplayName = "Purchase Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.PurchaseReturnValue)] = new() { DisplayName = "Purchase Return Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.KitchenIssueValue)] = new() { DisplayName = "Kitchen Issue Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.KitchenProductionValue)] = new() { DisplayName = "Kitchen Production Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.SaleValue)] = new() { DisplayName = "Sale Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.SaleReturnValue)] = new() { DisplayName = "Sale Return Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.StockTransferValue)] = new() { DisplayName = "Stock Transfer Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.BillValue)] = new() { DisplayName = "Bill Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.AdjustmentValue)] = new() { DisplayName = "Adjustment Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			// Per-type last & average rates
			[nameof(RawMaterialStockSummaryModel.LastPurchaseRate)] = new() { DisplayName = "Last Purchase Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AveragePurchaseRate)] = new() { DisplayName = "Avg Purchase Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastPurchaseReturnRate)] = new() { DisplayName = "Last Purchase Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AveragePurchaseReturnRate)] = new() { DisplayName = "Avg Purchase Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastKitchenIssueRate)] = new() { DisplayName = "Last Kitchen Issue Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageKitchenIssueRate)] = new() { DisplayName = "Avg Kitchen Issue Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastKitchenProductionRate)] = new() { DisplayName = "Last Kitchen Production Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageKitchenProductionRate)] = new() { DisplayName = "Avg Kitchen Production Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastSaleRate)] = new() { DisplayName = "Last Sale Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageSaleRate)] = new() { DisplayName = "Avg Sale Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastSaleReturnRate)] = new() { DisplayName = "Last Sale Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageSaleReturnRate)] = new() { DisplayName = "Avg Sale Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastStockTransferRate)] = new() { DisplayName = "Last Stock Transfer Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageStockTransferRate)] = new() { DisplayName = "Avg Stock Transfer Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastBillRate)] = new() { DisplayName = "Last Bill Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageBillRate)] = new() { DisplayName = "Avg Bill Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastAdjustmentRate)] = new() { DisplayName = "Last Adjustment Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.AverageAdjustmentRate)] = new() { DisplayName = "Avg Adjustment Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			// Recency & health
			[nameof(RawMaterialStockSummaryModel.AverageDailyConsumption)] = new() { DisplayName = "Avg Daily Use", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.DaysOnHand)] = new() { DisplayName = "Days On Hand", Format = "#,##0.0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.StockTurnoverRatio)] = new() { DisplayName = "Turnover", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.TransactionCount)] = new() { DisplayName = "Txns", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastPurchaseDate)] = new() { DisplayName = "Last Purchase", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastTransactionDate)] = new() { DisplayName = "Last Transaction", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.IsNegativeStock)] = new() { DisplayName = "Negative?", Alignment = CellAlignment.Center, IncludeInTotal = false, FormatCallback = value => new ReportFormatInfo { FormattedText = value is bool b && b ? "Yes" : "No" } }
		};

		// Core columns shown always; the rest only when full detail is requested.
		List<string> columnOrder =
		[
			nameof(RawMaterialStockSummaryModel.RawMaterialName),
			nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
			nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
			nameof(RawMaterialStockSummaryModel.OpeningStock),
			nameof(RawMaterialStockSummaryModel.InStock),
			nameof(RawMaterialStockSummaryModel.OutStock),
			nameof(RawMaterialStockSummaryModel.ClosingStock),
			nameof(RawMaterialStockSummaryModel.Rate),
			nameof(RawMaterialStockSummaryModel.ClosingValueByRate)
		];

		if (showAllColumns)
			columnOrder =
			[
				nameof(RawMaterialStockSummaryModel.RawMaterialName),
				nameof(RawMaterialStockSummaryModel.RawMaterialCode),
				nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
				nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),

				nameof(RawMaterialStockSummaryModel.OpeningStock),
				nameof(RawMaterialStockSummaryModel.InStock),
				nameof(RawMaterialStockSummaryModel.OutStock),
				nameof(RawMaterialStockSummaryModel.ClosingStock),
				nameof(RawMaterialStockSummaryModel.MonthlyStock),
				nameof(RawMaterialStockSummaryModel.PurchaseStock),
				nameof(RawMaterialStockSummaryModel.PurchaseReturnStock),
				nameof(RawMaterialStockSummaryModel.KitchenIssueStock),
				nameof(RawMaterialStockSummaryModel.KitchenProductionStock),
				nameof(RawMaterialStockSummaryModel.SaleStock),
				nameof(RawMaterialStockSummaryModel.SaleReturnStock),
				nameof(RawMaterialStockSummaryModel.StockTransferStock),
				nameof(RawMaterialStockSummaryModel.BillStock),
				nameof(RawMaterialStockSummaryModel.AdjustmentStock),

				nameof(RawMaterialStockSummaryModel.Rate),
				nameof(RawMaterialStockSummaryModel.ClosingValueByRate),
				nameof(RawMaterialStockSummaryModel.AverageInRate),
				nameof(RawMaterialStockSummaryModel.ClosingValueByAverageInRate),
				nameof(RawMaterialStockSummaryModel.AverageOutRate),
				nameof(RawMaterialStockSummaryModel.ClosingValueByAverageOutRate),
				nameof(RawMaterialStockSummaryModel.RateVariance),

				nameof(RawMaterialStockSummaryModel.OpeningValue),
				nameof(RawMaterialStockSummaryModel.TotalInValue),
				nameof(RawMaterialStockSummaryModel.TotalOutValue),
				nameof(RawMaterialStockSummaryModel.PurchaseValue),
				nameof(RawMaterialStockSummaryModel.PurchaseReturnValue),
				nameof(RawMaterialStockSummaryModel.KitchenIssueValue),
				nameof(RawMaterialStockSummaryModel.KitchenProductionValue),
				nameof(RawMaterialStockSummaryModel.SaleValue),
				nameof(RawMaterialStockSummaryModel.SaleReturnValue),
				nameof(RawMaterialStockSummaryModel.StockTransferValue),
				nameof(RawMaterialStockSummaryModel.BillValue),
				nameof(RawMaterialStockSummaryModel.AdjustmentValue),

				nameof(RawMaterialStockSummaryModel.LastPurchaseRate),
				nameof(RawMaterialStockSummaryModel.AveragePurchaseRate),
				nameof(RawMaterialStockSummaryModel.LastPurchaseReturnRate),
				nameof(RawMaterialStockSummaryModel.AveragePurchaseReturnRate),
				nameof(RawMaterialStockSummaryModel.LastKitchenIssueRate),
				nameof(RawMaterialStockSummaryModel.AverageKitchenIssueRate),
				nameof(RawMaterialStockSummaryModel.LastKitchenProductionRate),
				nameof(RawMaterialStockSummaryModel.AverageKitchenProductionRate),
				nameof(RawMaterialStockSummaryModel.LastSaleRate),
				nameof(RawMaterialStockSummaryModel.AverageSaleRate),
				nameof(RawMaterialStockSummaryModel.LastSaleReturnRate),
				nameof(RawMaterialStockSummaryModel.AverageSaleReturnRate),
				nameof(RawMaterialStockSummaryModel.LastStockTransferRate),
				nameof(RawMaterialStockSummaryModel.AverageStockTransferRate),
				nameof(RawMaterialStockSummaryModel.LastBillRate),
				nameof(RawMaterialStockSummaryModel.AverageBillRate),
				nameof(RawMaterialStockSummaryModel.LastAdjustmentRate),
				nameof(RawMaterialStockSummaryModel.AverageAdjustmentRate),

				nameof(RawMaterialStockSummaryModel.AverageDailyConsumption),
				nameof(RawMaterialStockSummaryModel.DaysOnHand),
				nameof(RawMaterialStockSummaryModel.StockTurnoverRatio),
				nameof(RawMaterialStockSummaryModel.TransactionCount),
				nameof(RawMaterialStockSummaryModel.LastPurchaseDate),
				nameof(RawMaterialStockSummaryModel.LastTransactionDate),
				nameof(RawMaterialStockSummaryModel.IsNegativeStock)
			];

		string fileName = $"RAW_MATERIAL_STOCK_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				stockData,
				"RAW MATERIAL STOCK REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				stockData,
				"RAW MATERIAL STOCK REPORT",
				"Stock Summary",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportDetailsReport(
		IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		var columnOrder = new List<string>
		{
			nameof(RawMaterialStockDetailsModel.TransactionDateTime),
			nameof(RawMaterialStockDetailsModel.TransactionNo),
			nameof(RawMaterialStockDetailsModel.Type),
			nameof(RawMaterialStockDetailsModel.RawMaterialName),
			nameof(RawMaterialStockDetailsModel.RawMaterialCode),
			nameof(RawMaterialStockDetailsModel.Quantity),
			nameof(RawMaterialStockDetailsModel.NetRate),
			nameof(RawMaterialStockDetailsModel.Total)
		};

		string fileName = $"RAW_MATERIAL_STOCK_DETAILS";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				stockDetailsData,
				"RAW MATERIAL STOCK DETAILS",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: false
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				stockDetailsData,
				"RAW MATERIAL STOCK DETAILS",
				"Transaction Details",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
