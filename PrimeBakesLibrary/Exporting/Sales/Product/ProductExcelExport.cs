using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster<T>(IEnumerable<T> productData)
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
				Rate = rate,
				Tax = tax,
				Remarks = remarks,
				Status = status is bool and true ? "Active" : "Deleted"
			};
		});

		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(ProductModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(ProductModel.Name)] = new() { DisplayName = "Product Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(ProductModel.Code)] = new() { DisplayName = "Product Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Category"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Tax"] = new() { DisplayName = "Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(ProductModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(ProductModel.Rate)] = new() { DisplayName = "Rate", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00" },
			[nameof(ProductModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
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

		var stream = await ExcelReportExportUtil.ExportToExcel(
			formattedData,
			"PRODUCT MASTER",
			"Product Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Product_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}
