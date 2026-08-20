using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Exports.Payroll.Masters;

public static class DesignationExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<DesignationModel> designationData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = designationData.Select(designation => new
		{
			designation.Id,
			designation.Name,
			designation.Code,
			designation.Remarks,
			Status = designation.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DesignationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DesignationModel.Name)] = new() { DisplayName = "Designation Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DesignationModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(DesignationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DesignationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DesignationModel.Id),
			nameof(DesignationModel.Name),
			nameof(DesignationModel.Code),
			nameof(DesignationModel.Remarks),
			nameof(DesignationModel.Status)
		];

		var fileName = $"Designation_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DESIGNATION MASTER",
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
				"DESIGNATION",
				"Designation Data",
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
