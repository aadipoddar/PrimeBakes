using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Masters;

namespace PrimeBakesLibrary.Exporting.Sales.Masters;

public static class ProductCategoryExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<ProductCategoryModel> productCategoryData,
		ReportExportType exportType)
	{
		var enrichedData = productCategoryData.Select(productCategory => new
		{
			productCategory.Id,
			productCategory.Name,
			productCategory.Remarks,
			Status = productCategory.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(ProductCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductCategoryModel.Name)] = new() { DisplayName = "Product Category Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(ProductCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(ProductCategoryModel.Id),
			nameof(ProductCategoryModel.Name),
			nameof(ProductCategoryModel.Remarks),
			nameof(ProductCategoryModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"ProductCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PRODUCT CATEGORY MASTER",
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
				"PRODUCT CATEGORY",
				"Product Category Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
