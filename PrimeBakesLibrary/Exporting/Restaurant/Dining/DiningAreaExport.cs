using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakesLibrary.Exporting.Restaurant.Dining;

public static class DiningAreaExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<DiningAreaModel> data,
		ReportExportType exportType,
		string locationName = null)
	{
		var enrichedData = data.Select(item => new
		{
			item.Id,
			item.Name,
			LocationName = locationName ?? "",
			item.Remarks,
			Status = item.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DiningAreaModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DiningAreaModel.Name)] = new() { DisplayName = "Dining Area Name", Alignment = CellAlignment.Left, IsRequired = true },
			["LocationName"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
			[nameof(DiningAreaModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DiningAreaModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DiningAreaModel.Id),
			nameof(DiningAreaModel.Name),
			"LocationName",
			nameof(DiningAreaModel.Remarks),
			nameof(DiningAreaModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"DiningArea_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DINING AREA MASTER",
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
				"DINING AREA",
				"Dining Area Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
