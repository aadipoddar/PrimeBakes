using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Store.Product;

public static class ProductLocationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<ProductLocationOverviewModel> productLocationData,
		ReportExportType exportType)
	{
		var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		
		var enrichedData = productLocationData.Select(pl => new
		{
			pl.Id,
			Location = locations.FirstOrDefault(l => l.Id == pl.LocationId)?.Name ?? "",
			ProductCode = pl.Code,
			ProductName = pl.Name,
			pl.Rate
		}).ToList();

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductCode"] = new() { DisplayName = "Product Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductName"] = new() { DisplayName = "Product Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductLocationModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
		};

		List<string> columnOrder =
		[
			"Location",
			"ProductCode",
			"ProductName",
			nameof(ProductLocationModel.Rate)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"ProductLocation_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PRODUCT LOCATION MASTER",
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
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"PRODUCT LOCATION MASTER",
				"Product Location Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
