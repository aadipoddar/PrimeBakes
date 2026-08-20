using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Exports.Payroll.Masters;

public static class EmployeeSalaryComponentExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<EmployeeSalaryComponentOverviewModel> employeeSalaryComponentData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = employeeSalaryComponentData.Select(employeeSalaryComponent => new
		{
			employeeSalaryComponent.Id,
			employeeSalaryComponent.EmployeeCode,
			employeeSalaryComponent.EmployeeName,
			employeeSalaryComponent.Sequence,
			employeeSalaryComponent.SalaryComponentCode,
			employeeSalaryComponent.SalaryComponentName,
			employeeSalaryComponent.SalaryComponentType,
			employeeSalaryComponent.Amount,
			Formula = employeeSalaryComponent.Formula ?? $"Master: {employeeSalaryComponent.SalaryComponentFormula ?? "Input"}",
			Prorate = employeeSalaryComponent.Prorate ? "Yes" : "No",
			employeeSalaryComponent.FromDate,
			employeeSalaryComponent.Remarks
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(EmployeeSalaryComponentOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeSalaryComponentOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(EmployeeSalaryComponentOverviewModel.EmployeeName)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(EmployeeSalaryComponentOverviewModel.Sequence)] = new() { DisplayName = "Seq", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentCode)] = new() { DisplayName = "Salary Component Code", Alignment = CellAlignment.Left },
			[nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentName)] = new() { DisplayName = "Salary Component Name", Alignment = CellAlignment.Left },
			[nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentType)] = new() { DisplayName = "Type", Alignment = CellAlignment.Left },
			[nameof(EmployeeSalaryComponentOverviewModel.Amount)] = new() { DisplayName = "Amount", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(EmployeeSalaryComponentOverviewModel.Formula)] = new() { DisplayName = "Formula", Alignment = CellAlignment.Left },
			[nameof(EmployeeSalaryComponentOverviewModel.Prorate)] = new() { DisplayName = "Prorate", Alignment = CellAlignment.Center },
			[nameof(EmployeeSalaryComponentOverviewModel.FromDate)] = new() { DisplayName = "Effective Date", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeSalaryComponentOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left }
		};

		List<string> columnOrder =
		[
			nameof(EmployeeSalaryComponentOverviewModel.Id),
			nameof(EmployeeSalaryComponentOverviewModel.EmployeeCode),
			nameof(EmployeeSalaryComponentOverviewModel.EmployeeName),
			nameof(EmployeeSalaryComponentOverviewModel.Sequence),
			nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentCode),
			nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentName),
			nameof(EmployeeSalaryComponentOverviewModel.SalaryComponentType),
			nameof(EmployeeSalaryComponentOverviewModel.Amount),
			nameof(EmployeeSalaryComponentOverviewModel.Formula),
			nameof(EmployeeSalaryComponentOverviewModel.Prorate),
			nameof(EmployeeSalaryComponentOverviewModel.FromDate),
			nameof(EmployeeSalaryComponentOverviewModel.Remarks)
		];

		var fileName = $"EmployeeSalaryComponent_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"EMPLOYEE SALARY COMPONENT MASTER",
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
				"EMPLOYEE SALARY COMPONENT",
				"Employee Salary Component Data",
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
