using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class GroupExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<GroupModel> groupData,
        ReportExportType exportType)
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

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(GroupModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(GroupModel.Name)] = new() { DisplayName = "Group Name", Alignment = CellAlignment.Left, IsRequired = true },
            ["Nature"] = new() { DisplayName = "Nature", Alignment = CellAlignment.Left },
            [nameof(GroupModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(GroupModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(GroupModel.Id),
            nameof(GroupModel.Name),
            "Nature",
            nameof(GroupModel.Remarks),
            nameof(GroupModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Group_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "GROUP MASTER",
                null,
                null,
                columnSettings,
                columnOrder,
                useBuiltInStyle: false,
                useLandscape: false
            );

            return (stream, fileName + ".pdf");
        }
        else
        {
            var stream = await ExcelReportExportUtil.ExportToExcel(
                enrichedData,
                "GROUP",
                "Group Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
