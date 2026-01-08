using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster<T>(
		IEnumerable<T> productData,
		ReportExportType exportType)
	{
		var formattedData = productData.Select(product =>
		{
			var props = typeof(T).GetProperties();
			var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(product);
			var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(product)?.ToString();
			var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(product)?.ToString();
			var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(product)?.ToString();
			var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(product);
			var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(product)?.ToString();
			var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(product)?.ToString();
			var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(product);

			return new
			{
				Id = id,
				Name = name,
				Code = code,
				Category = category,
				Rate = rate is decimal rateVal ? rateVal : 0m,
				Tax = tax,
				Remarks = remarks,
				Status = status is bool and true ? "Active" : "Deleted"
			};
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(ProductModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductModel.Name)] = new() { DisplayName = "Product Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductModel.Code)] = new() { DisplayName = "Product Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Category"] = new() { DisplayName = "Category", Alignment = CellAlignment.Left },
			[nameof(ProductModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00" },
			["Tax"] = new() { DisplayName = "Tax Code", Alignment = CellAlignment.Center },
			[nameof(ProductModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(ProductModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(ProductModel.Id),
			nameof(ProductModel.Name),
			nameof(ProductModel.Code),
			"Category",
			nameof(ProductModel.Rate),
			"Tax",
			nameof(ProductModel.Remarks),
			nameof(ProductModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Product_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				formattedData,
				"PRODUCT MASTER",
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
				formattedData,
				"PRODUCT MASTER",
				"Product Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
