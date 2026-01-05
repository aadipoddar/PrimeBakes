using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

public static class RawMaterialCategoryExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<RawMaterialCategoryModel> rawMaterialCategoryData)
	{
		var enrichedData = rawMaterialCategoryData.Select(rawMaterialCategory => new
		{
			rawMaterialCategory.Id,
			rawMaterialCategory.Name,
			rawMaterialCategory.Remarks,
			Status = rawMaterialCategory.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(RawMaterialCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			[nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			[nameof(RawMaterialCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(RawMaterialCategoryModel.Id),
			nameof(RawMaterialCategoryModel.Name),
			nameof(RawMaterialCategoryModel.Remarks),
			nameof(RawMaterialCategoryModel.Status)
		];

		var stream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"RAW MATERIAL CATEGORY",
			"Raw Material Category Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"RawMaterialCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}
