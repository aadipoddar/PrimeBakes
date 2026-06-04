using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class CompanyExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<CompanyModel> companyData,
		ReportExportType exportType)
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

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(CompanyModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(CompanyModel.Name)] = new() { DisplayName = "Company Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(CompanyModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["StateUT"] = new() { DisplayName = "State/UT", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.GSTNo)] = new() { DisplayName = "GST No", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.CINNo)] = new() { DisplayName = "CIN No", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Alias)] = new() { DisplayName = "Alias", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Phone)] = new() { DisplayName = "Phone", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Email)] = new() { DisplayName = "Email", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Address)] = new() { DisplayName = "Address", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(CompanyModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Company_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"COMPANY MASTER",
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
				"COMPANY",
				"Company Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
