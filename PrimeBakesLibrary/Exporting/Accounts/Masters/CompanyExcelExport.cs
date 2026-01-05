using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class CompanyExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<CompanyModel> companyData)
    {
        var stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

        var enrichedData = companyData.Select(company => new
        {
            company.Id,
            company.Name,
            company.Code,
            StateUT = stateUTs.FirstOrDefault(su => su.Id == company.StateUTId)?.Name ?? "N/A",
            company.GSTNo,
            company.PANNo,
            company.CINNo,
            company.Alias,
            company.Phone,
            company.Email,
            company.Address,
            company.Remarks,
            Status = company.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(CompanyModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(CompanyModel.Name)] = new() { DisplayName = "Company Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(CompanyModel.Code)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["StateUT"] = new() { DisplayName = "State/UT", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.GSTNo)] = new() { DisplayName = "GST No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.CINNo)] = new() { DisplayName = "CIN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.Alias)] = new() { DisplayName = "Alias", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.Phone)] = new() { DisplayName = "Phone", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.Email)] = new() { DisplayName = "Email", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.Address)] = new() { DisplayName = "Address", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(CompanyModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(CompanyModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(CompanyModel.Id),
            nameof(CompanyModel.Name),
            nameof(CompanyModel.Code),
            "StateUT",
            nameof(CompanyModel.GSTNo),
            nameof(CompanyModel.PANNo),
            nameof(CompanyModel.CINNo),
            nameof(CompanyModel.Alias),
            nameof(CompanyModel.Phone),
            nameof(CompanyModel.Email),
            nameof(CompanyModel.Address),
            nameof(CompanyModel.Remarks),
            nameof(CompanyModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "COMPANY",
            "Company Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"Company_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}
