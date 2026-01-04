using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

public static class UserExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<UserModel> userData)
    {
        var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

        var enrichedData = userData.Select(user => new
        {
            user.Id,
            user.Name,
            Passcode = user.Passcode.ToString("0000"),
            Location = locations.FirstOrDefault(l => l.Id == user.LocationId)?.Name ?? "N/A",
            Sales = user.Sales ? "Yes" : "No",
            Order = user.Order ? "Yes" : "No",
            Inventory = user.Inventory ? "Yes" : "No",
            Accounts = user.Accounts ? "Yes" : "No",
            Admin = user.Admin ? "Yes" : "No",
            user.Remarks,
            Status = user.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(UserModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Name)] = new() { DisplayName = "User Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(UserModel.Passcode)] = new() { DisplayName = "Passcode", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IsRequired = true },
            ["Location"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(UserModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(UserModel.Sales)] = new() { DisplayName = "Sales", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Order)] = new() { DisplayName = "Order", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Inventory)] = new() { DisplayName = "Inventory", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Accounts)] = new() { DisplayName = "Accounts", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Admin)] = new() { DisplayName = "Admin", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(UserModel.Id),
            nameof(UserModel.Name),
            nameof(UserModel.Passcode),
            "Location",
            nameof(UserModel.Sales),
            nameof(UserModel.Order),
            nameof(UserModel.Inventory),
            nameof(UserModel.Accounts),
            nameof(UserModel.Admin),
            nameof(UserModel.Remarks),
            nameof(UserModel.Status)
        ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "USER MASTER",
            "User Data",
            null,
            null,
            columnSettings,
            columnOrder
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"User_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}