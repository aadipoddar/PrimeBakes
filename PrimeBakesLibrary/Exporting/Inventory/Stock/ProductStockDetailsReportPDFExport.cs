using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class ProductStockDetailsReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<ProductStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(ProductStockDetailsModel.TransactionDateTime)] = new()
            {
                DisplayName = "Trans Date",
                Format = "dd-MMM-yyyy",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(ProductStockDetailsModel.TransactionNo)] = new()
            {
                DisplayName = "Trans No",
                IncludeInTotal = false
            },

            [nameof(ProductStockDetailsModel.Type)] = new()
            {
                DisplayName = "Trans Type",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(ProductStockDetailsModel.ProductName)] = new()
            {
                DisplayName = "Product",
                IncludeInTotal = false
            },

            [nameof(ProductStockDetailsModel.ProductCode)] = new()
            {
                DisplayName = "Code",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(ProductStockDetailsModel.Quantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(ProductStockDetailsModel.NetRate)] = new()
            {
                DisplayName = "Net Rate",
                Format = "#,##0.00",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            }
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

        var stream = await PDFReportExportUtil.ExportToPdf(
            stockDetailsData,
            "PRODUCT STOCK TRANSACTION DETAILS",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: false,
            new() { ["Location"] = location?.Name ?? null }
        );

        string fileName = $"PRODUCT_STOCK_DETAILS_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
