using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class ProductStockSummaryReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<ProductStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(ProductStockSummaryModel.ProductName)] = new() { DisplayName = "Product", IncludeInTotal = false },
            [nameof(ProductStockSummaryModel.ProductCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(ProductStockSummaryModel.ProductCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },

            [nameof(ProductStockSummaryModel.OpeningStock)] = new()
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

            [nameof(ProductStockSummaryModel.PurchaseStock)] = new()
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

            [nameof(ProductStockSummaryModel.SaleStock)] = new()
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

            [nameof(ProductStockSummaryModel.MonthlyStock)] = new()
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

            [nameof(ProductStockSummaryModel.ClosingStock)] = new()
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

            [nameof(ProductStockSummaryModel.Rate)] = new()
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

            [nameof(ProductStockSummaryModel.AveragePrice)] = new()
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

            [nameof(ProductStockSummaryModel.LastSalePrice)] = new()
            {
                DisplayName = "Last Sale Price",
                Format = "#,##0.00",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(ProductStockSummaryModel.LastSaleValue)] = new()
            {
                DisplayName = "Last Sale Value",
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

        var stream = await PDFReportExportUtil.ExportToPdf(
            stockData,
            "PRODUCT STOCK REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns,
            new() { ["Location"] = location?.Name ?? null }
        );

        string fileName = $"PRODUCT_STOCK_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
