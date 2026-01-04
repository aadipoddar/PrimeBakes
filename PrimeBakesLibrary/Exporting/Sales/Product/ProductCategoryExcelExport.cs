using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductCategoryExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<ProductCategoryModel> productCategoryData)
	{
		var enrichedData = productCategoryData.Select(productCategory => new
		{
			productCategory.Id,
			productCategory.Name,
			productCategory.Remarks,
			Status = productCategory.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(ProductCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(ProductCategoryModel.Name)] = new() { DisplayName = "Product Category Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(ProductCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(ProductCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(ProductCategoryModel.Id),
			nameof(ProductCategoryModel.Name),
			nameof(ProductCategoryModel.Remarks),
			nameof(ProductCategoryModel.Status)
		];

		var stream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"PRODUCT CATEGORY",
			"Product Category Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"ProductCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}
