using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Exports.Operations.User;

public static class UserExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<UserModel> userData,
		IEnumerable<LocationModel> locations,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = userData.Select(user => new
		{
			user.Id,
			user.Name,
			Passcode = user.Passcode.ToString("0000"),
			Location = locations.FirstOrDefault(l => l.Id == user.LocationId)?.Name ?? "N/A",
			ChangeProductFinancial = user.ChangeProductFinancial ? "Yes" : "No",
			Accounts = user.Accounts ? "Yes" : "No",
			Inventory = user.Inventory ? "Yes" : "No",
			Store = user.Store ? "Yes" : "No",
			Restaurant = user.Restaurant ? "Yes" : "No",
			Reports = user.Reports ? "Yes" : "No",
			Admin = user.Admin ? "Yes" : "No",
			user.Remarks,
			LastLoginTime = user.LastLoginTime?.ToString("dd-MMM-yyyy HH:mm") ?? "Not Logged In",
			Status = user.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(UserModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Name)] = new() { DisplayName = "User Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(UserModel.Passcode)] = new() { DisplayName = "Passcode", Alignment = CellAlignment.Center, IsRequired = true },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
			[nameof(UserModel.ChangeProductFinancial)] = new() { DisplayName = "Change Product Financials", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Accounts)] = new() { DisplayName = "Accounts", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Inventory)] = new() { DisplayName = "Inventory", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Store)] = new() { DisplayName = "Store", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Restaurant)] = new() { DisplayName = "Restaurant", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Reports)] = new() { DisplayName = "Reports", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Admin)] = new() { DisplayName = "Admin", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(UserModel.LastLoginTime)] = new() { DisplayName = "Last Login", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(UserModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(UserModel.Id),
			nameof(UserModel.Name),
			nameof(UserModel.Passcode),
			"Location",
			nameof(UserModel.ChangeProductFinancial),
			nameof(UserModel.Accounts),
			nameof(UserModel.Inventory),
			nameof(UserModel.Store),
			nameof(UserModel.Restaurant),
			nameof(UserModel.Reports),
			nameof(UserModel.Admin),
			nameof(UserModel.Remarks),
			nameof(UserModel.LastLoginTime),
			nameof(UserModel.Status)
		];

		var fileName = $"User_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"USER MASTER",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"USER MASTER",
				"User Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
