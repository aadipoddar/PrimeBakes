using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

public static class RawMaterialCategoryPDFExport
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

		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(RawMaterialCategoryModel.Id)] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", IncludeInTotal = false },
			[nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

			[nameof(RawMaterialCategoryModel.Status)] = new()
			{
				DisplayName = "Status",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			}
		};

		List<string> columnOrder =
		[
			nameof(RawMaterialCategoryModel.Id),
			nameof(RawMaterialCategoryModel.Name),
			nameof(RawMaterialCategoryModel.Remarks),
			nameof(RawMaterialCategoryModel.Status)
		];

		var stream = await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"RAW MATERIAL CATEGORY MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"RawMaterialCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
		return (stream, fileName);
	}
}
