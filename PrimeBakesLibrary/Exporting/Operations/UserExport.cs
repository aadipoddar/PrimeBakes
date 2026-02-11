using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Exporting.Operations;

public static class UserExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
        IEnumerable<UserModel> userData,
        ReportExportType exportType)
    {
        var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

        var enrichedData = userData.Select(user => new
        {
            user.Id,
            user.Name,
            Passcode = user.Passcode.ToString("0000"),
            Location = locations.FirstOrDefault(l => l.Id == user.LocationId)?.Name ?? "N/A",
            Order = user.Store ? "Yes" : "No",
            Inventory = user.Inventory ? "Yes" : "No",
            Accounts = user.Accounts ? "Yes" : "No",
            Reports = user.Reports ? "Yes" : "No",
            Admin = user.Admin ? "Yes" : "No",
            user.Remarks,
            Status = user.Status ? "Active" : "Deleted"
        });

        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(UserModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Name)] = new() { DisplayName = "User Name", Alignment = CellAlignment.Left, IsRequired = true },
            [nameof(UserModel.Passcode)] = new() { DisplayName = "Passcode", Alignment = CellAlignment.Center, IsRequired = true },
            ["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
            [nameof(UserModel.Store)] = new() { DisplayName = "Order", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Inventory)] = new() { DisplayName = "Inventory", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Reports)] = new() { DisplayName = "Reports", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Accounts)] = new() { DisplayName = "Accounts", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Admin)] = new() { DisplayName = "Admin", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(UserModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
            [nameof(UserModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
        };

        List<string> columnOrder =
        [
            nameof(UserModel.Id),
            nameof(UserModel.Name),
            nameof(UserModel.Passcode),
            "Location",
            nameof(UserModel.Store),
            nameof(UserModel.Inventory),
            nameof(UserModel.Accounts),
            nameof(UserModel.Reports),
            nameof(UserModel.Admin),
            nameof(UserModel.Remarks),
            nameof(UserModel.Status)
        ];

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        var fileName = $"User_Master_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                enrichedData,
                "USER MASTER",
                null,
                null,
                columnSettings,
                columnOrder,
                useBuiltInStyle: false,
                useLandscape: true
            );

            return (stream, fileName + ".pdf");
        }
        else
        {
            var stream = await ExcelReportExportUtil.ExportToExcel(
                enrichedData,
                "USER MASTER",
                "User Data",
                null,
                null,
                columnSettings,
                columnOrder
            );

            return (stream, fileName + ".xlsx");
        }
    }
}
