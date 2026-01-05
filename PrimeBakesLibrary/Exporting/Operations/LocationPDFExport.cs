using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

public static class LocationPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<LocationModel> locationData)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(LocationModel.Id)] = new() { DisplayName = "ID", IncludeInTotal = false },
            [nameof(LocationModel.Name)] = new() { DisplayName = "Location Name", IncludeInTotal = false },
            [nameof(LocationModel.PrefixCode)] = new() { DisplayName = "Prefix Code", IncludeInTotal = false },
            [nameof(LocationModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(LocationModel.Discount)] = new()
            {
                DisplayName = "Discount %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(LocationModel.Status)] = new()
            {
                DisplayName = "Status",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(LocationModel.Id),
            nameof(LocationModel.Name),
            nameof(LocationModel.PrefixCode),
            nameof(LocationModel.Discount),
            nameof(LocationModel.Remarks),
            nameof(LocationModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            locationData,
            "LOCATION MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Location_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
