using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Exports.Restaurant.Dining;

public static class DiningAreaExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<DiningAreaModel> diningAreaData,
		IEnumerable<LocationModel> locations,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = diningAreaData.Select(da => new
		{
			da.Id,
			da.Name,
			Location = locations.FirstOrDefault(l => l.Id == da.LocationId)?.Name ?? "N/A",
			da.Remarks,
			Status = da.Status ? "Active" : "Deleted"
		}).ToList();

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(DiningAreaModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(DiningAreaModel.Name)] = new() { DisplayName = "Dining Area Name", Alignment = CellAlignment.Left, IsRequired = true },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(DiningAreaModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(DiningAreaModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(DiningAreaModel.Id),
			nameof(DiningAreaModel.Name),
			"Location",
			nameof(DiningAreaModel.Remarks),
			nameof(DiningAreaModel.Status)
		];

		var fileName = $"DiningArea_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"DINING AREA MASTER",
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
				"DINING AREA MASTER",
				"Dining Area Data",
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
