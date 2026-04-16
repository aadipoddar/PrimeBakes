using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

public static class RawMaterialExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<RawMaterialModel> rawMaterialData,
		ReportExportType exportType)
	{
		var categories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterial);
		var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"RawMaterial_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"RAW MATERIAL MASTER",
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
				enrichedData,
				"RAW MATERIAL MASTER",
				"Raw Material Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
