using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Exporting.Operations;

public static class LocationExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<LocationModel> locationData,
        ReportExportType exportType)
    {
        var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

        var enrichedData = locationData.Select(location => new
        {
            location.Id,
            location.Name,
            location.Code,
            location.Discount,
            LedgerName = ledgers.FirstOrDefault(l => l.Id == location.LedgerId)?.Name ?? "N/A",
            location.Remarks,
            Status = location.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(LocationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(LocationModel.Name)] = new() { DisplayName = "Location Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(LocationModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IsRequired = true },
            [nameof(LocationModel.Discount)] = new() { DisplayName = "Discount %", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
            ["LedgerName"] = new() { DisplayName = "Ledger", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(LocationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(LocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(LocationModel.Id),
            nameof(LocationModel.Name),
            nameof(LocationModel.Code),
            nameof(LocationModel.Discount),
            "LedgerName",
            nameof(LocationModel.Remarks),
            nameof(LocationModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Location_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "LOCATION MASTER",
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
                "LOCATION MASTER",
                "Location Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
