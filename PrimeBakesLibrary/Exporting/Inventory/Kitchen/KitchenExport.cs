using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<KitchenModel> kitchenData,
		ReportExportType exportType)
	{
		var enrichedData = kitchenData.Select(kitchen => new
		{
			kitchen.Id,
			kitchen.Name,
			kitchen.Remarks,
			Status = kitchen.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenModel.Name)] = new() { DisplayName = "Kitchen Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(KitchenModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(KitchenModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(KitchenModel.Id),
			nameof(KitchenModel.Name),
			nameof(KitchenModel.Remarks),
			nameof(KitchenModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Kitchen_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"KITCHEN MASTER",
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
				"KITCHEN",
				"Kitchen Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
