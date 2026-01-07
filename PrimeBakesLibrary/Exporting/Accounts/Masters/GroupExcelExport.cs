using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class GroupExcelExport
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

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(GroupModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(GroupModel.Name)] = new() { DisplayName = "Group Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["Nature"] = new() { DisplayName = "Nature", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(GroupModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(GroupModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(GroupModel.Id),
            nameof(GroupModel.Name),
            "Nature",
            nameof(GroupModel.Remarks),
            nameof(GroupModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "GROUP",
            "Group Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Group_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
