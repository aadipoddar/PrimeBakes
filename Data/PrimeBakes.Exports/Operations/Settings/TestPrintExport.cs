using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;

using SkiaSharp;

namespace PrimeBakes.Exports.Operations.Settings;

public static class TestPrintExport
{
	public static byte[] GenerateTestReceipt(string printerName, string printerAddress, string platform, CompanyModel company, DateTime currentDateTime)
	{
		using var bitmap = RenderReceipt(printerName, printerAddress, platform, company, currentDateTime);
		return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
	}

	public static byte[] GenerateTestReceiptPng(string printerName, string printerAddress, string platform, CompanyModel company, DateTime currentDateTime)
	{
		using var bitmap = RenderReceipt(printerName, printerAddress, platform, company, currentDateTime);
		return ThermalPrintUtil.BitmapToPngBytes(bitmap);
	}

	private static SKBitmap RenderReceipt(string printerName, string printerAddress, string platform, CompanyModel company, DateTime currentDateTime)
	{

		int width = ThermalPrintUtil.PaperDots80mm;
		int maxHeight = 1200;
		using var tempBitmap = new SKBitmap(width, maxHeight);
		using var canvas = new SKCanvas(tempBitmap);
		canvas.Clear(SKColors.White);

		float y = ThermalPrintUtil.Margin;

		y = ThermalPrintUtil.DrawLogo(canvas, width, y);

		if (company is not null)
		{
			y = ThermalPrintUtil.DrawCompanyHeader(canvas, company, width, y);
		}
		else
		{
			y = ThermalPrintUtil.DrawCenteredText(canvas, "PRIME BAKES", width, y, ThermalPrintUtil.FontSizeTitle, bold: true);
			y += ThermalPrintUtil.SectionGap;
		}

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

		y = ThermalPrintUtil.DrawCenteredText(canvas, "--- Test Print ---", width, y, ThermalPrintUtil.FontSizeHeader, bold: true);
		y += ThermalPrintUtil.SectionGap;

		y = ThermalPrintUtil.DrawLabelValueBlock(canvas,
		[
			("Printer", string.IsNullOrWhiteSpace(printerName) ? "N/A" : printerName),
			("Address", string.IsNullOrWhiteSpace(printerAddress) ? "N/A" : printerAddress),
			("Date", currentDateTime.ToString("dd MMM yyyy  hh:mm tt")),
			("Platform", string.IsNullOrWhiteSpace(platform) ? "N/A" : platform),
		], width, y);
		y += ThermalPrintUtil.SectionGap;

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

		y = ThermalPrintUtil.DrawCenteredText(canvas, "Thanks. Visit Again", width, y, ThermalPrintUtil.FontSizeNormal, bold: false);
		y += ThermalPrintUtil.LineGap;
		y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);
		y += ThermalPrintUtil.Margin;

		return ThermalPrintUtil.CropBitmap(tempBitmap, width, (int)Math.Ceiling(y));
	}

}
