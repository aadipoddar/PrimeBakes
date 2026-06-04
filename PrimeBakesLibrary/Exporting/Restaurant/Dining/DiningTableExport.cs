using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakesLibrary.Exporting.Restaurant.Dining;

public static class DiningTableExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DiningTableModel> data,
		ReportExportType exportType,
		string diningAreaName = null)
	{
		var enrichedData = data.Select(item => new
		{
			item.Id,
			item.Name,
			DiningAreaName = diningAreaName ?? "",
			item.Remarks,
			Status = item.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DiningTableModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DiningTableModel.Name)] = new() { DisplayName = "Table Name", Alignment = CellAlignment.Left, IsRequired = true },
			["DiningAreaName"] = new() { DisplayName = "Dining Area", Alignment = CellAlignment.Left },
			[nameof(DiningTableModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DiningTableModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DiningTableModel.Id),
			nameof(DiningTableModel.Name),
			"DiningAreaName",
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
				"DINING TABLE",
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
