using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

public static class RawMaterialStockDetailsReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new()
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

            [nameof(RawMaterialStockDetailsModel.TransactionNo)] = new()
            {
                DisplayName = "Trans No",
                IncludeInTotal = false
            },

            [nameof(RawMaterialStockDetailsModel.Type)] = new()
            {
                DisplayName = "Trans Type",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new()
            {
                DisplayName = "Raw Material",
                IncludeInTotal = false
            },

            [nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new()
            {
                DisplayName = "Code",
                IncludeInTotal = false,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(RawMaterialStockDetailsModel.Quantity)] = new()
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

            [nameof(RawMaterialStockDetailsModel.NetRate)] = new()
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
            nameof(RawMaterialStockDetailsModel.TransactionDateTime),
            nameof(RawMaterialStockDetailsModel.TransactionNo),
            nameof(RawMaterialStockDetailsModel.Type),
            nameof(RawMaterialStockDetailsModel.RawMaterialName),
            nameof(RawMaterialStockDetailsModel.RawMaterialCode),
            nameof(RawMaterialStockDetailsModel.Quantity),
            nameof(RawMaterialStockDetailsModel.NetRate)
        };

        var stream = await PDFReportExportUtil.ExportToPdf(
            stockDetailsData,
            "RAW MATERIAL STOCK TRANSACTION DETAILS",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: false
        );

        string fileName = $"RAW_MATERIAL_STOCK_DETAILS_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
