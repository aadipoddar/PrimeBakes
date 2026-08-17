using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Product;

public static class ProductCategoryExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<ProductCategoryModel> productCategoryData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = productCategoryData.Select(productCategory => new
		{
			productCategory.Id,
			productCategory.Name,
			ShowInMenu = productCategory.ShowInMenu ? "Yes" : "No",
			productCategory.Remarks,
			Status = productCategory.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(ProductCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductCategoryModel.Name)] = new() { DisplayName = "Product Category Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(ProductCategoryModel.ShowInMenu)] = new() { DisplayName = "Show in Menu", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(ProductCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(ProductCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(ProductCategoryModel.Id),
			nameof(ProductCategoryModel.Name),
			nameof(ProductCategoryModel.ShowInMenu),
			nameof(ProductCategoryModel.Remarks),
			nameof(ProductCategoryModel.Status)
		];

		var fileName = $"ProductCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PRODUCT CATEGORY MASTER",
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
				"PRODUCT CATEGORY",
				"Product Category Data",
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
