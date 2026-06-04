using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class AccountTypeExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<AccountTypeModel> accountTypeData,
        ReportExportType exportType)
    {
        var enrichedData = accountTypeData.Select(accountType => new
        {
            accountType.Id,
            accountType.Name,
            accountType.Remarks,
            Status = accountType.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(AccountTypeModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountTypeModel.Name)] = new() { DisplayName = "Account Type Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(AccountTypeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(AccountTypeModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(AccountTypeModel.Id),
            nameof(AccountTypeModel.Name),
            nameof(AccountTypeModel.Remarks),
            nameof(AccountTypeModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Account_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "ACCOUNT TYPE MASTER",
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
                "ACCOUNT TYPE",
                "Account Type Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
