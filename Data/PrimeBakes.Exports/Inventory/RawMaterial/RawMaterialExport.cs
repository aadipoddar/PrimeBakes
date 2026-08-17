using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Inventory.RawMaterial;

public static class RawMaterialExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<RawMaterialModel> rawMaterialData,
		IEnumerable<RawMaterialCategoryModel> categories,
		IEnumerable<TaxModel> taxes,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = rawMaterialData.Select(rm => new
		{
			rm.Id,
			rm.Name,
			rm.Code,
			Category = categories.FirstOrDefault(c => c.Id == rm.RawMaterialCategoryId)?.Name ?? "N/A",
			rm.Rate,
			rm.UnitOfMeasurement,
			Tax = taxes.FirstOrDefault(t => t.Id == rm.TaxId)?.Code ?? "N/A",
			rm.Remarks,
			rm.Status
		}).ToList();

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RawMaterialModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialModel.Name)] = new() { DisplayName = "Raw Material Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(RawMaterialModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Category"] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RawMaterialModel.Rate)] = new() { DisplayName = "Rate", Alignment = CellAlignment.Right, Format = "0.00", IncludeInTotal = false },
			[nameof(RawMaterialModel.UnitOfMeasurement)] = new() { DisplayName = "Unit", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["Tax"] = new() { DisplayName = "Tax Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RawMaterialModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(RawMaterialModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(RawMaterialModel.Id),
			nameof(RawMaterialModel.Name),
			nameof(RawMaterialModel.Code),
			"Category",
			nameof(RawMaterialModel.Rate),
			nameof(RawMaterialModel.UnitOfMeasurement),
			"Tax",
			nameof(RawMaterialModel.Remarks),
			nameof(RawMaterialModel.Status)
		];

		var fileName = $"RawMaterial_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"RAW MATERIAL MASTER",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"RAW MATERIAL MASTER",
				"Raw Material Data",
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
