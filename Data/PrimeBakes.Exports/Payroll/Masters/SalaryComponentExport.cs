using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Exports.Payroll.Masters;

public static class SalaryComponentExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<SalaryComponentModel> salaryComponentData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = salaryComponentData.Select(salaryComponent => new
		{
			salaryComponent.Id,
			salaryComponent.Sequence,
			salaryComponent.Name,
			salaryComponent.Code,
			salaryComponent.ComponentType,
			Formula = salaryComponent.Formula ?? "Input",
			Prorate = salaryComponent.Prorate ? "Yes" : "No",
			Rounding = salaryComponent.Rounding ? "Yes" : "No",
			ShowOnPayslip = salaryComponent.ShowOnPayslip ? "Yes" : "No",
			salaryComponent.Remarks,
			Status = salaryComponent.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SalaryComponentModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SalaryComponentModel.Sequence)] = new() { DisplayName = "Seq", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SalaryComponentModel.Name)] = new() { DisplayName = "Component Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(SalaryComponentModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(SalaryComponentModel.ComponentType)] = new() { DisplayName = "Type", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.Formula)] = new() { DisplayName = "Formula", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.Prorate)] = new() { DisplayName = "Prorate", Alignment = CellAlignment.Center },
			[nameof(SalaryComponentModel.Rounding)] = new() { DisplayName = "Rounding", Alignment = CellAlignment.Center },
			[nameof(SalaryComponentModel.ShowOnPayslip)] = new() { DisplayName = "On Payslip", Alignment = CellAlignment.Center },
			[nameof(SalaryComponentModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(SalaryComponentModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(SalaryComponentModel.Id),
			nameof(SalaryComponentModel.Sequence),
			nameof(SalaryComponentModel.Name),
			nameof(SalaryComponentModel.Code),
			nameof(SalaryComponentModel.ComponentType),
			nameof(SalaryComponentModel.Formula),
			nameof(SalaryComponentModel.Prorate),
			nameof(SalaryComponentModel.Rounding),
			nameof(SalaryComponentModel.ShowOnPayslip),
			nameof(SalaryComponentModel.Remarks),
			nameof(SalaryComponentModel.Status)
		];

		var fileName = $"SalaryComponent_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"SALARY COMPONENT MASTER",
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
				"SALARY COMPONENT",
				"Salary Component Data",
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
