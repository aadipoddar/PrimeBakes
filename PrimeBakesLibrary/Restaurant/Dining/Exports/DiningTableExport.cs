using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Restaurant.Dining.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Restaurant.Dining.Exports;

public static class DiningTableExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DiningTableModel> diningTableData,
		ReportExportType exportType)
	{
		var diningAreas = await CommonData.LoadTableData<DiningAreaModel>(RestaurantNames.DiningArea);

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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"DiningTable_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DINING TABLE MASTER",
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
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"DINING TABLE MASTER",
				"Dining Table Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
