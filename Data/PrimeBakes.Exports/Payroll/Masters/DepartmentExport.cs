using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Exports.Payroll.Masters;

public static class DepartmentExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<DepartmentModel> departmentData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = departmentData.Select(department => new
		{
			department.Id,
			department.Name,
			department.Code,
			department.Remarks,
			Status = department.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DepartmentModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DepartmentModel.Name)] = new() { DisplayName = "Department Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DepartmentModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DepartmentModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DepartmentModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DepartmentModel.Id),
			nameof(DepartmentModel.Name),
			nameof(DepartmentModel.Code),
			nameof(DepartmentModel.Remarks),
			nameof(DepartmentModel.Status)
		];

		var fileName = $"Department_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DEPARTMENT MASTER",
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
				"DEPARTMENT",
				"Department Data",
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
