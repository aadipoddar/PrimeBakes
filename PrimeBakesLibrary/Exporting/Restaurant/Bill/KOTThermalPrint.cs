using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Product;

using SkiaSharp;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

public static class KOTThermalPrint
{
	public static async Task<byte[]> GenerateThermalBill(int billId, int kotCategoryId, List<BillItemCartModel> kotItems)
	{
		using var bitmap = await RenderReceipt(billId, kotCategoryId, kotItems);
		return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
	}
	public static async Task<byte[]> GenerateThermalBillPng(int billId, int kotCategoryId, List<BillItemCartModel> kotItems)
	{
		using var bitmap = await RenderReceipt(billId, kotCategoryId, kotItems);
		return ThermalPrintUtil.BitmapToPngBytes(bitmap);
	}

	private static async Task<SKBitmap?> RenderReceipt(int billId, int kotCategoryId, List<BillItemCartModel> kotItems)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);

		if (kotItems.Count == 0)
			return null;

		int width = ThermalPrintUtil.PaperDots80mm;
		int maxHeight = 3000;
		using var tempBitmap = new SKBitmap(width, maxHeight);
		using var canvas = new SKCanvas(tempBitmap);
		canvas.Clear(SKColors.White);

		float y = ThermalPrintUtil.Margin;

		y = DrawHeader(canvas, width, y);
		y = await DrawBillDetails(canvas, bill, kotCategoryId, width, y);
		y = await DrawItems(canvas, kotItems, width, y);
		y = await DrawFooter(canvas, bill, width, y);

		y += ThermalPrintUtil.Margin;

		await BillData.MarkKOTAsPrinted(bill.Id);

		return ThermalPrintUtil.CropBitmap(tempBitmap, width, (int)Math.Ceiling(y));
	}

	private static float DrawHeader(SKCanvas canvas, int width, float y)
	{
		y = ThermalPrintUtil.DrawCenteredText(canvas, "KOT", width, y, ThermalPrintUtil.FontSizeTitle, bold: true);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawBillDetails(SKCanvas canvas, BillModel bill, int kotCategoryId, int width, float y)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);
		var kotCategory = await CommonData.LoadTableDataById<KOTCategoryModel>(TableNames.KOTCategory, kotCategoryId);

		var pairs = new List<(string Label, string Value)>
		{
			("Outlet",   location?.Name ?? "N/A"),
			("Bill No",  bill.TransactionNo),
			("Date",     bill.TransactionDateTime.ToString("dd/MMM/yy hh:mm tt")),
			("Table No", table?.Name ?? "N/A"),
			("KOT Category", kotCategory?.Name ?? "N/A")
		};

		y = ThermalPrintUtil.DrawLabelValueBlock(canvas, pairs, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawItems(SKCanvas canvas, List<BillItemCartModel> kotItems, int width, float y)
	{
		var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		bool hasNotes = kotItems.Any(i => !string.IsNullOrWhiteSpace(i.Remarks));

		string[] headers;
		SKTextAlign[] alignments;
		float[] columnPercents;

		if (hasNotes)
		{
			headers = ["Item", "Qty", "Notes"];
			alignments = [SKTextAlign.Left, SKTextAlign.Right, SKTextAlign.Left];
			columnPercents = [0.50f, 0.15f, 0.35f];
		}
		else
		{
			headers = ["Item", "Qty"];
			alignments = [SKTextAlign.Left, SKTextAlign.Right];
			columnPercents = [0.78f, 0.22f];
		}

		var rows = new List<string[]>();
		foreach (var item in kotItems)
		{
			string productName = products.FirstOrDefault(p => p.Id == item.ItemId)?.Name ?? "Unknown";
			rows.Add(hasNotes
				? [productName, item.Quantity.FormatSmartDecimal(), item.Remarks ?? string.Empty]
				: [productName, item.Quantity.FormatSmartDecimal()]);
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
		y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed By: {printedBy} | On: {currentDateTime:dd/MMM/yy hh:mm tt}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);
		return y;
	}
}
