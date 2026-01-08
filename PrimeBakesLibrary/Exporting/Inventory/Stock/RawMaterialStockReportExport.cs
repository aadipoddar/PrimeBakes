using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

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
			[nameof(RawMaterialStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new() { DisplayName = "Last Purchase Price", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new() { DisplayName = "Last Purchase Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(RawMaterialStockSummaryModel.RawMaterialName),
				nameof(RawMaterialStockSummaryModel.RawMaterialCode),
				nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
				nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
				nameof(RawMaterialStockSummaryModel.OpeningStock),
				nameof(RawMaterialStockSummaryModel.PurchaseStock),
				nameof(RawMaterialStockSummaryModel.SaleStock),
				nameof(RawMaterialStockSummaryModel.MonthlyStock),
				nameof(RawMaterialStockSummaryModel.ClosingStock),
				nameof(RawMaterialStockSummaryModel.Rate),
				nameof(RawMaterialStockSummaryModel.ClosingValue),
				nameof(RawMaterialStockSummaryModel.AveragePrice),
				nameof(RawMaterialStockSummaryModel.WeightedAverageValue),
				nameof(RawMaterialStockSummaryModel.LastPurchasePrice),
				nameof(RawMaterialStockSummaryModel.LastPurchaseValue)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(RawMaterialStockSummaryModel.RawMaterialName),
				nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
				nameof(RawMaterialStockSummaryModel.OpeningStock),
				nameof(RawMaterialStockSummaryModel.PurchaseStock),
				nameof(RawMaterialStockSummaryModel.SaleStock),
				nameof(RawMaterialStockSummaryModel.ClosingStock),
				nameof(RawMaterialStockSummaryModel.Rate),
				nameof(RawMaterialStockSummaryModel.ClosingValue)
			];
		}

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
			[nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(RawMaterialStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		var columnOrder = new List<string>
		{
			nameof(RawMaterialStockDetailsModel.TransactionDateTime),
			nameof(RawMaterialStockDetailsModel.TransactionNo),
			nameof(RawMaterialStockDetailsModel.Type),
			nameof(RawMaterialStockDetailsModel.RawMaterialName),
			nameof(RawMaterialStockDetailsModel.RawMaterialCode),
			nameof(RawMaterialStockDetailsModel.Quantity),
			nameof(RawMaterialStockDetailsModel.NetRate)
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
