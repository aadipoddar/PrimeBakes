using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Exports.Restaurant.Dining;

public static class DiningTableExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<DiningTableModel> diningTableData,
		IEnumerable<DiningAreaModel> diningAreas,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = diningTableData.Select(dt => new
		{
			dt.Id,
			dt.Name,
			DiningArea = diningAreas.FirstOrDefault(da => da.Id == dt.DiningAreaId)?.Name ?? "N/A",
			dt.Remarks,
			Status = dt.Status ? "Active" : "Deleted"
		}).ToList();

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DiningTableModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DiningTableModel.Name)] = new() { DisplayName = "Table Name", Alignment = CellAlignment.Left, IsRequired = true },
			["DiningArea"] = new() { DisplayName = "Dining Area", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(DiningTableModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DiningTableModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DiningTableModel.Id),
			nameof(DiningTableModel.Name),
			"DiningArea",
			nameof(DiningTableModel.Remarks),
			nameof(DiningTableModel.Status)
		];

		var fileName = $"DiningTable_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DINING TABLE MASTER",
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
				"DINING TABLE MASTER",
				"Dining Table Data",
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
