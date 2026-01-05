using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenPDFExport
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

		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(KitchenModel.Id)] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(KitchenModel.Name)] = new() { DisplayName = "Kitchen Name", IncludeInTotal = false },
			[nameof(KitchenModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

			[nameof(KitchenModel.Status)] = new()
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
			nameof(KitchenModel.Id),
			nameof(KitchenModel.Name),
			nameof(KitchenModel.Remarks),
			nameof(KitchenModel.Status)
		];

		var stream = await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"KITCHEN MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);

		var currentDateTime = CommonData.LoadCurrentDateTime();
		var fileName = $"Kitchen_Master_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
		return (stream, fileName);
	}
}
