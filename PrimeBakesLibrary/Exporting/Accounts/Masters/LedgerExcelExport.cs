using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class LedgerExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<LedgerModel> ledgerData)
    {
        var groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
        var accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
        var stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

        var enrichedData = ledgerData.Select(ledger => new
        {
            ledger.Id,
            ledger.Name,
            ledger.Code,
            Group = groups.FirstOrDefault(g => g.Id == ledger.GroupId)?.Name ?? "N/A",
            AccountType = accountTypes.FirstOrDefault(at => at.Id == ledger.AccountTypeId)?.Name ?? "N/A",
            StateUT = stateUTs.FirstOrDefault(su => su.Id == ledger.StateUTId)?.Name ?? "N/A",
            ledger.GSTNo,
            ledger.PANNo,
            ledger.CINNo,
            ledger.Alias,
            ledger.Phone,
            ledger.Email,
            ledger.Address,
            ledger.Remarks,
            Status = ledger.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(LedgerModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(LedgerModel.Name)] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(LedgerModel.Code)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["Group"] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["AccountType"] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["StateUT"] = new() { DisplayName = "State/UT", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(LedgerModel.GSTNo)] = new() { DisplayName = "GST No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.CINNo)] = new() { DisplayName = "CIN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.Alias)] = new() { DisplayName = "Alias", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.Phone)] = new() { DisplayName = "Phone", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.Email)] = new() { DisplayName = "Email", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.Address)] = new() { DisplayName = "Address", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(LedgerModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(LedgerModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(LedgerModel.Id),
            nameof(LedgerModel.Name),
            nameof(LedgerModel.Code),
            "Group",
            "AccountType",
            "StateUT",
            nameof(LedgerModel.GSTNo),
            nameof(LedgerModel.PANNo),
            nameof(LedgerModel.CINNo),
            nameof(LedgerModel.Alias),
            nameof(LedgerModel.Phone),
            nameof(LedgerModel.Email),
            nameof(LedgerModel.Address),
            nameof(LedgerModel.Remarks),
            nameof(LedgerModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "LEDGER",
            "Ledger Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Ledger_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
