using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class ProductStockReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<ProductStockSummaryModel> stockData,
        IEnumerable<ProductStockDetailsModel> stockDetailsData = null,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(ProductStockSummaryModel.ProductName)] = new() { DisplayName = "Product", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 25 },
            [nameof(ProductStockSummaryModel.ProductCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockSummaryModel.ProductCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 20 },
            [nameof(ProductStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 },
            [nameof(ProductStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 },
            [nameof(ProductStockSummaryModel.LastSalePrice)] = new() { DisplayName = "Last Sale Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 18 },
            [nameof(ProductStockSummaryModel.LastSaleValue)] = new() { DisplayName = "Last Sale Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 }
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

        if (stockDetailsData == null || !stockDetailsData.Any())
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

            string fileName = $"PRODUCT_STOCK_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            return (stream, fileName);
        }

        return await ExportWithDetails(
            stockData,
            stockDetailsData,
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            location
        );
    }

    private static async Task<(MemoryStream stream, string fileName)> ExportWithDetails(
        IEnumerable<ProductStockSummaryModel> stockData,
        IEnumerable<ProductStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart,
        DateOnly? dateRangeEnd,
        Dictionary<string, ExcelReportExportUtil.ColumnSetting> summaryColumnSettings,
        List<string> summaryColumnOrder,
        LocationModel location = null)
    {
        var summaryStream = await ExcelReportExportUtil.ExportToExcel(
            stockData,
            "PRODUCT STOCK REPORT",
            "Stock Summary",
            dateRangeStart,
            dateRangeEnd,
            summaryColumnSettings,
            summaryColumnOrder,
            new() { ["Locattion"] = location?.Name ?? null }
        );

        var detailsColumnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(ProductStockDetailsModel.ProductName)] = new() { DisplayName = "Product", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 25 },
            [nameof(ProductStockDetailsModel.ProductCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 18 },
            [nameof(ProductStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 18 },
            [nameof(ProductStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 }
        };

        var detailsColumnOrder = new List<string>
        {
            nameof(ProductStockDetailsModel.TransactionDateTime),
            nameof(ProductStockDetailsModel.TransactionNo),
            nameof(ProductStockDetailsModel.Type),
            nameof(ProductStockDetailsModel.ProductName),
            nameof(ProductStockDetailsModel.ProductCode),
            nameof(ProductStockDetailsModel.Quantity),
            nameof(ProductStockDetailsModel.NetRate)
        };

        var detailsStream = await ExcelReportExportUtil.ExportToExcel(
            stockDetailsData,
            "PRODUCT STOCK DETAILS",
            "Transaction Details",
            dateRangeStart,
            dateRangeEnd,
            detailsColumnSettings,
            detailsColumnOrder,
            new() { ["Location"] = location?.Name ?? null }
        );

        var combinedStream = CombineWorksheets(summaryStream, detailsStream);

        string fileName = $"PRODUCT_STOCK_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (combinedStream, fileName);
    }

    private static MemoryStream CombineWorksheets(MemoryStream summaryStream, MemoryStream detailsStream)
    {
        using var excelEngine = new Syncfusion.XlsIO.ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = Syncfusion.XlsIO.ExcelVersion.Xlsx;

        var workbook = application.Workbooks.Open(summaryStream);
        var detailsWorkbook = application.Workbooks.Open(detailsStream);

        workbook.Worksheets.AddCopy(detailsWorkbook.Worksheets[0]);
        detailsWorkbook.Close();

        var combinedStream = new MemoryStream();
        workbook.SaveAs(combinedStream);
        combinedStream.Position = 0;

        summaryStream.Dispose();
        detailsStream.Dispose();

        return combinedStream;
    }
}
