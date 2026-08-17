using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Product;

public static class ProductLocationExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<ProductLocationOverviewModel> productLocationData,
		IEnumerable<LocationModel> locations,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = productLocationData.Select(pl => new
		{
			pl.Id,
			Location = locations.FirstOrDefault(l => l.Id == pl.LocationId)?.Name ?? "",
			ProductCode = pl.Code,
			ProductName = pl.Name,
			pl.Rate,
			pl.FromDate
		}).ToList();

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductCode"] = new() { DisplayName = "Product Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductName"] = new() { DisplayName = "Product Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductLocationModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(ProductLocationOverviewModel.FromDate)] = new() { DisplayName = "Effective Date", Alignment = CellAlignment.Center, Format = "dd-MMM-yyyy", IncludeInTotal = false },
		};

		List<string> columnOrder =
		[
			"Location",
			"ProductCode",
			"ProductName",
			nameof(ProductLocationModel.Rate),
			nameof(ProductLocationOverviewModel.FromDate)
		];

		var fileName = $"ProductLocation_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PRODUCT LOCATION MASTER",
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
				"PRODUCT LOCATION MASTER",
				"Product Location Data",
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
