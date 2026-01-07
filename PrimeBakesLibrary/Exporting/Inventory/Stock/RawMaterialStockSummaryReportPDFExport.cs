using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class RawMaterialStockSummaryReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new() { DisplayName = "Raw Material", IncludeInTotal = false },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
            [nameof(RawMaterialStockSummaryModel.UnitOfMeasurement)] = new()
            {
                DisplayName = "UOM",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.OpeningStock)] = new()
            {
                DisplayName = "Opening Stock",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new()
            {
                DisplayName = "Purchase Stock",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.SaleStock)] = new()
            {
                DisplayName = "Sale Stock",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new()
            {
                DisplayName = "Monthly Stock",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.ClosingStock)] = new()
            {
                DisplayName = "Closing Stock",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.Rate)] = new()
            {
                DisplayName = "Rate",
                Format = "#,##0.00",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.AveragePrice)] = new()
            {
                DisplayName = "Average Price",
                Format = "#,##0.00",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new()
            {
                DisplayName = "Last Purchase Price",
                Format = "#,##0.00",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new()
            {
                DisplayName = "Last Purchase Value",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            }
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

        var stream = await PDFReportExportUtil.ExportToPdf(
            stockData,
            "RAW MATERIAL STOCK REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns
        );

        string fileName = $"RAW_MATERIAL_STOCK_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
