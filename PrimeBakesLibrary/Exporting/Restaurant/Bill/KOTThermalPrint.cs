using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Product;

using SkiaSharp;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

/// <summary>
/// Renders a KOT (Kitchen Order Ticket) as a SkiaSharp raster bitmap and converts it
/// to ESC/POS byte data ready for Bluetooth thermal printing.
/// </summary>
public static class KOTThermalPrint
{
	/// <summary>
	/// Generates ESC/POS raster bytes for a KOT thermal receipt.
	/// </summary>
	/// <param name="billId">The bill ID whose pending KOT items should be printed.</param>
	/// <returns>ESC/POS byte array, or empty array if there are no pending KOT items.</returns>
	public static async Task<byte[]> GenerateThermalBill(int billId)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);
		var kotItems = billDetails.Where(d => d.KOTPrint).ToList();

		if (kotItems.Count == 0)
			return [];

		int width = ThermalPrintUtil.PaperDots80mm;
		int maxHeight = 3000;
		using var tempBitmap = new SKBitmap(width, maxHeight);
		using var canvas = new SKCanvas(tempBitmap);
		canvas.Clear(SKColors.White);

		float y = ThermalPrintUtil.Margin;

		y = DrawHeader(canvas, width, y);
		y = await DrawBillDetails(canvas, bill, width, y);
		y = await DrawItems(canvas, kotItems, width, y);
		y = await DrawFooter(canvas, bill, width, y);

		y += ThermalPrintUtil.Margin;

		await BillData.MarkKOTAsPrinted(bill.Id);

		using var cropped = ThermalPrintUtil.CropBitmap(tempBitmap, width, (int)Math.Ceiling(y));
		return ThermalPrintUtil.ConvertBitmapToThermalBytes(cropped, width);
	}

	private static float DrawHeader(SKCanvas canvas, int width, float y)
	{
		y = ThermalPrintUtil.DrawCenteredText(canvas, "KOT", width, y, ThermalPrintUtil.FontSizeTitle, bold: true);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawBillDetails(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		var pairs = new List<(string Label, string Value)>
		{
			("Outlet",   location?.Name ?? "N/A"),
			("Bill No",  bill.TransactionNo),
			("Date",     bill.TransactionDateTime.ToString("dd/MM/yy hh:mm tt")),
			("Table No", table?.Name ?? "N/A")
		};

		y = ThermalPrintUtil.DrawLabelValueBlock(canvas, pairs, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawItems(SKCanvas canvas, List<BillDetailModel> kotItems, int width, float y)
	{
		var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		string[] headers = ["Item", "Qty"];
		SKTextAlign[] alignments = [SKTextAlign.Left, SKTextAlign.Right];
		float[] columnPercents = [0.78f, 0.22f];

		var rows = new List<string[]>();
		foreach (var item in kotItems)
		{
			string productName = products.FirstOrDefault(p => p.Id == item.ProductId)?.Name ?? "Unknown";
			rows.Add([productName, item.Quantity.FormatSmartDecimal()]);

			if (!string.IsNullOrWhiteSpace(item.Remarks))
				rows.Add([$"  [Note] {item.Remarks}", string.Empty]);
		}

		y = ThermalPrintUtil.DrawTable(canvas, headers, alignments, columnPercents, rows, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawFooter(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		string printedBy = users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name ?? "Unknown";
		y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed By: {printedBy} | On: {currentDateTime:dd/MM/yy hh:mm tt}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);
		return y;
	}
}
