using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Product;

using SkiaSharp;

namespace PrimeBakes.Exports.Restaurant.Bill;

public static class KOTThermalPrint
{
	public static byte[] GenerateThermalBill(KOTThermalBundle bundle, List<BillItemCartModel> kotItems)
	{
		using var bitmap = RenderReceipt(bundle, kotItems);
		return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
	}
	public static byte[] GenerateThermalBillPng(KOTThermalBundle bundle, List<BillItemCartModel> kotItems)
	{
		using var bitmap = RenderReceipt(bundle, kotItems);
		return ThermalPrintUtil.BitmapToPngBytes(bitmap);
	}

	private static SKBitmap? RenderReceipt(KOTThermalBundle bundle, List<BillItemCartModel> kotItems)
	{
		var (bill, kotCategory, currentDateTime) = bundle;

		if (kotItems.Count == 0)
			return null;

		int width = ThermalPrintUtil.PaperDots80mm;
		int maxHeight = 3000;
		using var tempBitmap = new SKBitmap(width, maxHeight);
		using var canvas = new SKCanvas(tempBitmap);
		canvas.Clear(SKColors.White);

		float y = ThermalPrintUtil.Margin;

		y = DrawHeader(canvas, width, y);
		y = DrawBillDetails(canvas, bill, kotCategory, width, y);
		y = DrawItems(canvas, kotItems, width, y);
		y = DrawFooter(canvas, bill, currentDateTime, width, y);

		y += ThermalPrintUtil.Margin;

		return ThermalPrintUtil.CropBitmap(tempBitmap, width, (int)Math.Ceiling(y));
	}

	private static float DrawHeader(SKCanvas canvas, int width, float y)
	{
		y = ThermalPrintUtil.DrawCenteredText(canvas, "KOT", width, y, ThermalPrintUtil.FontSizeTitle, bold: true);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static float DrawBillDetails(SKCanvas canvas, BillOverviewModel bill, KOTCategoryModel kotCategory, int width, float y)
	{
		var pairs = new List<(string Label, string Value)>
		{
			("Outlet",   bill.LocationName),
			("Bill No",  bill.TransactionNo),
			("Date",     bill.TransactionDateTime.ToString("dd/MMM/yy hh:mm tt")),
			("Table No", bill.DiningTableName),
			("KOT Category", kotCategory?.Name ?? "N/A")
		};

		y = ThermalPrintUtil.DrawLabelValueBlock(canvas, pairs, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static float DrawItems(SKCanvas canvas, List<BillItemCartModel> kotItems, int width, float y)
	{
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
			rows.Add(hasNotes
				? [item.ItemName, item.Quantity.FormatSmartDecimal(), item.Remarks ?? string.Empty]
				: [item.ItemName, item.Quantity.FormatSmartDecimal()]);
		}

		y = ThermalPrintUtil.DrawTable(canvas, headers, alignments, columnPercents, rows, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static float DrawFooter(SKCanvas canvas, BillOverviewModel bill, DateTime currentDateTime, int width, float y)
	{
		y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed By: {bill.CreatedByName}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed On: {currentDateTime:dd/MMM/yy hh:mm tt}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);
		return y;
	}
}
