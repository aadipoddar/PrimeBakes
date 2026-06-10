using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Inventory.Stock.Exports;

public static class ProductStockReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportSummaryReport(
		IEnumerable<ProductStockSummaryModel> stockData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(ProductStockSummaryModel.ProductName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ProductCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ProductCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			// Stock quantities
			[nameof(ProductStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.InStock)] = new() { DisplayName = "Stock In", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(ProductStockSummaryModel.OutStock)] = new() { DisplayName = "Stock Out", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(ProductStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Net Movement", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.PurchaseReturnStock)] = new() { DisplayName = "Purchase Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.KitchenIssueStock)] = new() { DisplayName = "Kitchen Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.KitchenProductionStock)] = new() { DisplayName = "Kitchen Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.SaleReturnStock)] = new() { DisplayName = "Sale Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.StockTransferStock)] = new() { DisplayName = "Stock Transfer", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.BillStock)] = new() { DisplayName = "Bill", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.AdjustmentStock)] = new() { DisplayName = "Adjustment", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			// Rate & valuation
			[nameof(ProductStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ClosingValueByRate)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.AverageInRate)] = new() { DisplayName = "Avg In Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ClosingValueByAverageInRate)] = new() { DisplayName = "Value at Avg In", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.AverageOutRate)] = new() { DisplayName = "Avg Out Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ClosingValueByAverageOutRate)] = new() { DisplayName = "Value at Avg Out", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.RateVariance)] = new() { DisplayName = "Rate Variance", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			// Movement values (₹)
			[nameof(ProductStockSummaryModel.OpeningValue)] = new() { DisplayName = "Opening Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.TotalInValue)] = new() { DisplayName = "Total In Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.TotalOutValue)] = new() { DisplayName = "Total Out Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.PurchaseValue)] = new() { DisplayName = "Purchase Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.PurchaseReturnValue)] = new() { DisplayName = "Purchase Return Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.KitchenIssueValue)] = new() { DisplayName = "Kitchen Issue Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.KitchenProductionValue)] = new() { DisplayName = "Kitchen Production Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.SaleValue)] = new() { DisplayName = "Sale Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.SaleReturnValue)] = new() { DisplayName = "Sale Return Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.StockTransferValue)] = new() { DisplayName = "Stock Transfer Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.BillValue)] = new() { DisplayName = "Bill Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.AdjustmentValue)] = new() { DisplayName = "Adjustment Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			// Per-type last & average rates
			[nameof(ProductStockSummaryModel.LastPurchaseRate)] = new() { DisplayName = "Last Purchase Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AveragePurchaseRate)] = new() { DisplayName = "Avg Purchase Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastPurchaseReturnRate)] = new() { DisplayName = "Last Purchase Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AveragePurchaseReturnRate)] = new() { DisplayName = "Avg Purchase Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastKitchenIssueRate)] = new() { DisplayName = "Last Kitchen Issue Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageKitchenIssueRate)] = new() { DisplayName = "Avg Kitchen Issue Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastKitchenProductionRate)] = new() { DisplayName = "Last Kitchen Production Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageKitchenProductionRate)] = new() { DisplayName = "Avg Kitchen Production Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastSaleRate)] = new() { DisplayName = "Last Sale Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageSaleRate)] = new() { DisplayName = "Avg Sale Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastSaleReturnRate)] = new() { DisplayName = "Last Sale Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageSaleReturnRate)] = new() { DisplayName = "Avg Sale Return Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastStockTransferRate)] = new() { DisplayName = "Last Stock Transfer Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageStockTransferRate)] = new() { DisplayName = "Avg Stock Transfer Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastBillRate)] = new() { DisplayName = "Last Bill Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageBillRate)] = new() { DisplayName = "Avg Bill Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastAdjustmentRate)] = new() { DisplayName = "Last Adjustment Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.AverageAdjustmentRate)] = new() { DisplayName = "Avg Adjustment Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			// Recency & health
			[nameof(ProductStockSummaryModel.AverageDailyConsumption)] = new() { DisplayName = "Avg Daily Use", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.DaysOnHand)] = new() { DisplayName = "Days On Hand", Format = "#,##0.0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.StockTurnoverRatio)] = new() { DisplayName = "Turnover", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.TransactionCount)] = new() { DisplayName = "Txns", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastSaleDate)] = new() { DisplayName = "Last Sale", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastTransactionDate)] = new() { DisplayName = "Last Transaction", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.IsNegativeStock)] = new() { DisplayName = "Negative?", Alignment = CellAlignment.Center, IncludeInTotal = false, FormatCallback = value => new ReportFormatInfo { FormattedText = value is bool b && b ? "Yes" : "No" } }
		};

		// Core columns shown always; the rest only when full detail is requested.
		List<string> columnOrder =
		[
			nameof(ProductStockSummaryModel.ProductName),
			nameof(ProductStockSummaryModel.ProductCategoryName),
			nameof(ProductStockSummaryModel.OpeningStock),
			nameof(ProductStockSummaryModel.InStock),
			nameof(ProductStockSummaryModel.OutStock),
			nameof(ProductStockSummaryModel.ClosingStock),
			nameof(ProductStockSummaryModel.Rate),
			nameof(ProductStockSummaryModel.ClosingValueByRate)
		];

		if (showAllColumns)
			columnOrder =
			[
				nameof(ProductStockSummaryModel.ProductName),
				nameof(ProductStockSummaryModel.ProductCode),
				nameof(ProductStockSummaryModel.ProductCategoryName),

				nameof(ProductStockSummaryModel.OpeningStock),
				nameof(ProductStockSummaryModel.InStock),
				nameof(ProductStockSummaryModel.OutStock),
				nameof(ProductStockSummaryModel.ClosingStock),
				nameof(ProductStockSummaryModel.MonthlyStock),
				nameof(ProductStockSummaryModel.PurchaseStock),
				nameof(ProductStockSummaryModel.PurchaseReturnStock),
				nameof(ProductStockSummaryModel.KitchenIssueStock),
				nameof(ProductStockSummaryModel.KitchenProductionStock),
				nameof(ProductStockSummaryModel.SaleStock),
				nameof(ProductStockSummaryModel.SaleReturnStock),
				nameof(ProductStockSummaryModel.StockTransferStock),
				nameof(ProductStockSummaryModel.BillStock),
				nameof(ProductStockSummaryModel.AdjustmentStock),

				nameof(ProductStockSummaryModel.Rate),
				nameof(ProductStockSummaryModel.ClosingValueByRate),
				nameof(ProductStockSummaryModel.AverageInRate),
				nameof(ProductStockSummaryModel.ClosingValueByAverageInRate),
				nameof(ProductStockSummaryModel.AverageOutRate),
				nameof(ProductStockSummaryModel.ClosingValueByAverageOutRate),
				nameof(ProductStockSummaryModel.RateVariance),

				nameof(ProductStockSummaryModel.OpeningValue),
				nameof(ProductStockSummaryModel.TotalInValue),
				nameof(ProductStockSummaryModel.TotalOutValue),
				nameof(ProductStockSummaryModel.PurchaseValue),
				nameof(ProductStockSummaryModel.PurchaseReturnValue),
				nameof(ProductStockSummaryModel.KitchenIssueValue),
				nameof(ProductStockSummaryModel.KitchenProductionValue),
				nameof(ProductStockSummaryModel.SaleValue),
				nameof(ProductStockSummaryModel.SaleReturnValue),
				nameof(ProductStockSummaryModel.StockTransferValue),
				nameof(ProductStockSummaryModel.BillValue),
				nameof(ProductStockSummaryModel.AdjustmentValue),

				nameof(ProductStockSummaryModel.LastPurchaseRate),
				nameof(ProductStockSummaryModel.AveragePurchaseRate),
				nameof(ProductStockSummaryModel.LastPurchaseReturnRate),
				nameof(ProductStockSummaryModel.AveragePurchaseReturnRate),
				nameof(ProductStockSummaryModel.LastKitchenIssueRate),
				nameof(ProductStockSummaryModel.AverageKitchenIssueRate),
				nameof(ProductStockSummaryModel.LastKitchenProductionRate),
				nameof(ProductStockSummaryModel.AverageKitchenProductionRate),
				nameof(ProductStockSummaryModel.LastSaleRate),
				nameof(ProductStockSummaryModel.AverageSaleRate),
				nameof(ProductStockSummaryModel.LastSaleReturnRate),
				nameof(ProductStockSummaryModel.AverageSaleReturnRate),
				nameof(ProductStockSummaryModel.LastStockTransferRate),
				nameof(ProductStockSummaryModel.AverageStockTransferRate),
				nameof(ProductStockSummaryModel.LastBillRate),
				nameof(ProductStockSummaryModel.AverageBillRate),
				nameof(ProductStockSummaryModel.LastAdjustmentRate),
				nameof(ProductStockSummaryModel.AverageAdjustmentRate),

				nameof(ProductStockSummaryModel.AverageDailyConsumption),
				nameof(ProductStockSummaryModel.DaysOnHand),
				nameof(ProductStockSummaryModel.StockTurnoverRatio),
				nameof(ProductStockSummaryModel.TransactionCount),
				nameof(ProductStockSummaryModel.LastSaleDate),
				nameof(ProductStockSummaryModel.LastTransactionDate),
				nameof(ProductStockSummaryModel.IsNegativeStock)
			];

		string fileName = $"PRODUCT_STOCK_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				stockData,
				"PRODUCT STOCK REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true,
				new() { ["Location"] = location?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				stockData,
				"PRODUCT STOCK REPORT",
				"Stock Summary",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Location"] = location?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportDetailsReport(
		IEnumerable<ProductStockDetailsModel> stockDetailsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(ProductStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.ProductName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.ProductCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockDetailsModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		var columnOrder = new List<string>
		{
			nameof(ProductStockDetailsModel.TransactionDateTime),
			nameof(ProductStockDetailsModel.TransactionNo),
			nameof(ProductStockDetailsModel.Type),
			nameof(ProductStockDetailsModel.ProductName),
			nameof(ProductStockDetailsModel.ProductCode),
			nameof(ProductStockDetailsModel.Quantity),
			nameof(ProductStockDetailsModel.NetRate),
			nameof(ProductStockDetailsModel.Total)
		};

		string fileName = $"PRODUCT_STOCK_DETAILS";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				stockDetailsData,
				"PRODUCT STOCK DETAILS",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: false,
				new() { ["Location"] = location?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				stockDetailsData,
				"PRODUCT STOCK DETAILS",
				"Transaction Details",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Location"] = location?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}
