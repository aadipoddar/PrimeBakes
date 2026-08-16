using PrimeBakes.Library.Common;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Settings;

using SkiaSharp;

namespace PrimeBakes.Library.Operations.Settings.Exports;

public static class TestPrintExport
{
	public static async Task<byte[]> GenerateTestReceipt(string printerName, string printerAddress, string platform)
	{
		using var bitmap = await RenderReceipt(printerName, printerAddress, platform);
		return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
	}

	public static async Task<byte[]> GenerateTestReceiptPng(string printerName, string printerAddress, string platform)
	{
		using var bitmap = await RenderReceipt(printerName, printerAddress, platform);
		return ThermalPrintUtil.BitmapToPngBytes(bitmap);
	}

	private static async Task<SKBitmap> RenderReceipt(string printerName, string printerAddress, string platform)
	{
		var company = await LoadPrimaryCompany();
		var currentDateTime = await CommonData.LoadCurrentDateTime();

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

	private static async Task<CompanyModel> LoadPrimaryCompany()
	{
		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);

		return setting is not null && int.TryParse(setting.Value, out var companyId)
			? await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, companyId)
			: null;
	}
}
