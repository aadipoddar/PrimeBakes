using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

public static class LocationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<LocationModel> locationData,
		ReportExportType exportType)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(LocationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(LocationModel.Name)] = new() { DisplayName = "Location Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LocationModel.PrefixCode)] = new() { DisplayName = "Prefix Code", Alignment = CellAlignment.Center, IsRequired = true },
			[nameof(LocationModel.Discount)] = new() { DisplayName = "Discount %", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(LocationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(LocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(LocationModel.Id),
			nameof(LocationModel.Name),
			nameof(LocationModel.PrefixCode),
			nameof(LocationModel.Discount),
			nameof(LocationModel.Remarks),
			nameof(LocationModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Location_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				locationData,
				"LOCATION MASTER",
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
				locationData,
				"LOCATION MASTER",
				"Location Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
