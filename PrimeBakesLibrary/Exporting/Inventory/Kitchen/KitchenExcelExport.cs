using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<KitchenModel> kitchenData)
	{
		var enrichedData = kitchenData.Select(kitchenData => new
		{
			kitchenData.Id,
			kitchenData.Name,
			kitchenData.Remarks,
			Status = kitchenData.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(KitchenModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			[nameof(KitchenModel.Name)] = new() { DisplayName = "Kitchen Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(KitchenModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			[nameof(KitchenModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};


		List<string> columnOrder =
		[
			nameof(KitchenModel.Id),
			nameof(KitchenModel.Name),
			nameof(KitchenModel.Remarks),
			nameof(KitchenModel.Status)
		];

		var stream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"KITCHEN",
			"Kitchen Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"Kitchen_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}
