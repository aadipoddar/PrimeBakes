using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class AccountTypeExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<AccountTypeModel> accountTypeData)
    {
        var enrichedData = accountTypeData.Select(accountType => new
        {
            accountType.Id,
            accountType.Name,
            accountType.Remarks,
            Status = accountType.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(AccountTypeModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(AccountTypeModel.Name)] = new() { DisplayName = "Account Type Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(AccountTypeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(AccountTypeModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };


        List<string> columnOrder =
        [
            nameof(AccountTypeModel.Id),
            nameof(AccountTypeModel.Name),
            nameof(AccountTypeModel.Remarks),
            nameof(AccountTypeModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "ACCOUNT TYPE",
            "Account Type Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Account_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
