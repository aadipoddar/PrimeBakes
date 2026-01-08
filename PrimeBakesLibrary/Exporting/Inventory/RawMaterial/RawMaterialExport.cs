using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

public static class RawMaterialExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster<T>(
		IEnumerable<T> rawMaterialData,
		ReportExportType exportType)
	{
		var enrichedData = rawMaterialData.Select(rm =>
		{
			var props = typeof(T).GetProperties();
			var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(rm);
			var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(rm)?.ToString();
			var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(rm)?.ToString();
			var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(rm)?.ToString();
			var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(rm);
			var unit = props.FirstOrDefault(p => p.Name == "UnitOfMeasurement")?.GetValue(rm)?.ToString();
			var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(rm)?.ToString();
			var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(rm)?.ToString();
			var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(rm);

			return new
			{
				Id = id,
				Name = name,
				Code = code,
				Category = category,
				Rate = rate is decimal rateVal ? rateVal : 0m,
				UnitOfMeasurement = unit,
				Tax = tax,
				Remarks = remarks,
				Status = status is bool and true ? "Active" : "Deleted"
			};
		});

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
