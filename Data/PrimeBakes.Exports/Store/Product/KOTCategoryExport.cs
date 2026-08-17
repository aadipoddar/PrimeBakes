using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Product;

public static class KOTCategoryExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<KOTCategoryModel> kotCategoryData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = kotCategoryData.Select(kotCategory => new
		{
			kotCategory.Id,
			kotCategory.Name,
			kotCategory.Remarks,
			Status = kotCategory.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KOTCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KOTCategoryModel.Name)] = new() { DisplayName = "KOT Category Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(KOTCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(KOTCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(KOTCategoryModel.Id),
			nameof(KOTCategoryModel.Name),
			nameof(KOTCategoryModel.Remarks),
			nameof(KOTCategoryModel.Status)
		];

		var fileName = $"KOTCategory_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"KOT CATEGORY MASTER",
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
				"KOT CATEGORY",
				"KOT Category Data",
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
