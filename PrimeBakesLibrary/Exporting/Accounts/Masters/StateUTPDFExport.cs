using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class StateUTPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<StateUTModel> stateUTData)
    {
        var enrichedData = stateUTData.Select(stateUT => new
        {
            stateUT.Id,
            stateUT.Name,
            stateUT.Remarks,
            UnionTerritory = stateUT.UnionTerritory ? "Yes" : "No",
            Status = stateUT.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(StateUTModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(StateUTModel.Name)] = new() { DisplayName = "State/UT Name", IncludeInTotal = false },

            [nameof(StateUTModel.UnionTerritory)] = new()
            {
                DisplayName = "Union Territory",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(StateUTModel.Status)] = new()
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

        List<string> columnOrder =
        [
            nameof(StateUTModel.Id),
            nameof(StateUTModel.Name),
            nameof(StateUTModel.Remarks),
            nameof(StateUTModel.UnionTerritory),
            nameof(StateUTModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "STATE & UNION TERRITORY MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"StateUT_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
