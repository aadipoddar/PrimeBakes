using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Exports.Accounts.Masters;

public static class AccountTypeExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<AccountTypeModel> accountTypeData,
		DateTime currentDateTime,
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

		var fileName = $"Account_Type_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"ACCOUNT TYPE MASTER",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"ACCOUNT TYPE",
				"Account Type Data",
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
