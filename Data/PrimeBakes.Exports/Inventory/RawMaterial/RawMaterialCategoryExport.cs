using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Exports.Inventory.RawMaterial;

public static class RawMaterialCategoryExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<RawMaterialCategoryModel> rawMaterialCategoryData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = rawMaterialCategoryData.Select(rawMaterialCategory => new
		{
			rawMaterialCategory.Id,
			rawMaterialCategory.Name,
			rawMaterialCategory.Remarks,
			Status = rawMaterialCategory.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RawMaterialCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(RawMaterialCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(RawMaterialCategoryModel.Id),
			nameof(RawMaterialCategoryModel.Name),
			nameof(RawMaterialCategoryModel.Remarks),
			nameof(RawMaterialCategoryModel.Status)
		];

		var fileName = $"RawMaterialCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"RAW MATERIAL CATEGORY MASTER",
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
				"RAW MATERIAL CATEGORY",
				"Raw Material Category Data",
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
