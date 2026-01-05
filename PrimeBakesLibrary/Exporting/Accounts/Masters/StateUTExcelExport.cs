using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class StateUTExcelExport
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

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(StateUTModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(StateUTModel.Name)] = new() { DisplayName = "State/UT Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(StateUTModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(StateUTModel.UnionTerritory)] = new() { DisplayName = "Union Territory", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(StateUTModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(StateUTModel.Id),
            nameof(StateUTModel.Name),
            nameof(StateUTModel.Remarks),
            nameof(StateUTModel.UnionTerritory),
            nameof(StateUTModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "STATE & UNION TERRITORY",
            "State UT Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"StateUT_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
