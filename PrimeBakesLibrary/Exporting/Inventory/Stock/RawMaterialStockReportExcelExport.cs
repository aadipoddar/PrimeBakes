using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class RawMaterialStockReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 25 },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 20 },
            [nameof(RawMaterialStockSummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 10 },
            [nameof(RawMaterialStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 },
            [nameof(RawMaterialStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 },
            [nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new() { DisplayName = "Last Purchase Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 18 },
            [nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new() { DisplayName = "Last Purchase Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 }
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

        if (stockDetailsData == null || !stockDetailsData.Any())
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

            string fileName = $"RAW_MATERIAL_STOCK_REPORT";
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
            columnOrder
        );
    }

    private static async Task<(MemoryStream stream, string fileName)> ExportWithDetails(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart,
        DateOnly? dateRangeEnd,
        Dictionary<string, ExcelReportExportUtil.ColumnSetting> summaryColumnSettings,
        List<string> summaryColumnOrder)
    {
        var summaryStream = await ExcelReportExportUtil.ExportToExcel(
            stockData,
            "RAW MATERIAL STOCK REPORT",
            "Stock Summary",
            dateRangeStart,
            dateRangeEnd,
            summaryColumnSettings,
            summaryColumnOrder
        );

        var detailsColumnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 25 },
            [nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(RawMaterialStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false, Width = 18 },
            [nameof(RawMaterialStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 18 },
            [nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(RawMaterialStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 }
        };

        var detailsColumnOrder = new List<string>
        {
            nameof(RawMaterialStockDetailsModel.TransactionDateTime),
            nameof(RawMaterialStockDetailsModel.TransactionNo),
            nameof(RawMaterialStockDetailsModel.Type),
            nameof(RawMaterialStockDetailsModel.RawMaterialName),
            nameof(RawMaterialStockDetailsModel.RawMaterialCode),
            nameof(RawMaterialStockDetailsModel.Quantity),
            nameof(RawMaterialStockDetailsModel.NetRate)
        };

        var detailsStream = await ExcelReportExportUtil.ExportToExcel(
            stockDetailsData,
            "RAW MATERIAL STOCK DETAILS",
            "Transaction Details",
            dateRangeStart,
            dateRangeEnd,
            detailsColumnSettings,
            detailsColumnOrder
        );

        var combinedStream = CombineWorksheets(summaryStream, detailsStream);

        string fileName = $"RAW_MATERIAL_STOCK_REPORT";
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
