using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Store.Product;

public static class ProductLocationExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster<T>(
		IEnumerable<T> productLocationData,
		ReportExportType exportType)
	{
		var props = typeof(T).GetProperties();

		var enrichedData = productLocationData.Select(productLocation =>
		{
			var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(productLocation);
			var location = props.FirstOrDefault(p => p.Name == "Location")?.GetValue(productLocation)?.ToString();
			var productCode = props.FirstOrDefault(p => p.Name == "ProductCode")?.GetValue(productLocation)?.ToString();
			var productName = props.FirstOrDefault(p => p.Name == "ProductName")?.GetValue(productLocation)?.ToString();
			var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(productLocation);
			var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(productLocation);

			return new
			{
				Id = id,
				Location = location,
				ProductCode = productCode,
				ProductName = productName,
				Rate = rate is decimal rateVal ? rateVal : 0m,
				Status = status is bool and true ? "Active" : "Deleted"
			};
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductCode"] = new() { DisplayName = "Product Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			["ProductName"] = new() { DisplayName = "Product Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductLocationModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(ProductLocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			"Location",
			"ProductCode",
			"ProductName",
			nameof(ProductLocationModel.Rate),
			nameof(ProductLocationModel.Status)
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
