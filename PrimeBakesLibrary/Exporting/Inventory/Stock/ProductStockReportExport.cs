using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

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
			[nameof(ProductStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(ProductStockSummaryModel.LastSalePrice)] = new() { DisplayName = "Last Sale Price", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(ProductStockSummaryModel.LastSaleValue)] = new() { DisplayName = "Last Sale Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(ProductStockSummaryModel.ProductName),
				nameof(ProductStockSummaryModel.ProductCode),
				nameof(ProductStockSummaryModel.ProductCategoryName),
				nameof(ProductStockSummaryModel.OpeningStock),
				nameof(ProductStockSummaryModel.PurchaseStock),
				nameof(ProductStockSummaryModel.SaleStock),
				nameof(ProductStockSummaryModel.MonthlyStock),
				nameof(ProductStockSummaryModel.ClosingStock),
				nameof(ProductStockSummaryModel.Rate),
				nameof(ProductStockSummaryModel.ClosingValue),
				nameof(ProductStockSummaryModel.AveragePrice),
				nameof(ProductStockSummaryModel.WeightedAverageValue),
				nameof(ProductStockSummaryModel.LastSalePrice),
				nameof(ProductStockSummaryModel.LastSaleValue)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(ProductStockSummaryModel.ProductName),
				nameof(ProductStockSummaryModel.OpeningStock),
				nameof(ProductStockSummaryModel.PurchaseStock),
				nameof(ProductStockSummaryModel.SaleStock),
				nameof(ProductStockSummaryModel.ClosingStock),
				nameof(ProductStockSummaryModel.Rate),
				nameof(ProductStockSummaryModel.ClosingValue)
			];
		}

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
			[nameof(ProductStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		var columnOrder = new List<string>
		{
			nameof(ProductStockDetailsModel.TransactionDateTime),
			nameof(ProductStockDetailsModel.TransactionNo),
			nameof(ProductStockDetailsModel.Type),
			nameof(ProductStockDetailsModel.ProductName),
			nameof(ProductStockDetailsModel.ProductCode),
			nameof(ProductStockDetailsModel.Quantity),
			nameof(ProductStockDetailsModel.NetRate)
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
