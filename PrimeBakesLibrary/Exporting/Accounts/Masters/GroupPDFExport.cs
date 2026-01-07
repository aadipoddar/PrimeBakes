using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class GroupPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<GroupModel> groupData)
    {
        var natures = await CommonData.LoadTableData<NatureModel>(TableNames.Nature);

        var enrichedData = groupData.Select(group => new
        {
            group.Id,
            group.Name,
            Nature = natures.FirstOrDefault(n => n.Id == group.NatureId)?.Name,
            group.Remarks,
            Status = group.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(GroupModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(GroupModel.Name)] = new() { DisplayName = "Group Name", IncludeInTotal = false },
            ["Nature"] = new() { DisplayName = "Nature", IncludeInTotal = false },
            [nameof(GroupModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(GroupModel.Status)] = new()
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
            nameof(GroupModel.Id),
            nameof(GroupModel.Name),
            "Nature",
            nameof(GroupModel.Remarks),
            nameof(GroupModel.Status)
        ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "GROUP MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Group_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
