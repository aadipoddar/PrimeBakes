using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Product;

using SkiaSharp;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

public static class BillThermalPrint
{
	public static async Task<byte[]> GenerateThermalBill(int billId)
	{
		using var bitmap = await RenderReceipt(billId);
		return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
	}

	public static async Task<byte[]> GenerateThermalBillPng(int billId)
	{
		using var bitmap = await RenderReceipt(billId);
		return ThermalPrintUtil.BitmapToPngBytes(bitmap);
	}

	private static async Task<SKBitmap> RenderReceipt(int billId)
	{
		var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billId);

		int width = ThermalPrintUtil.PaperDots80mm;
		int maxHeight = 3000;
		using var tempBitmap = new SKBitmap(width, maxHeight);
		using var canvas = new SKCanvas(tempBitmap);
		canvas.Clear(SKColors.White);

		float y = ThermalPrintUtil.Margin;

		y = await DrawCompanyHeader(canvas, width, y);
		y = await DrawBillDetails(canvas, bill, width, y);
		y = await DrawItemDetails(canvas, bill, width, y);
		y = await DrawTotalDetails(canvas, bill, width, y);
		y = DrawPaymentModes(canvas, bill, width, y);
		y = await DrawFooter(canvas, bill, width, y);

		y += ThermalPrintUtil.Margin;

		return ThermalPrintUtil.CropBitmap(tempBitmap, width, (int)Math.Ceiling(y));
	}

	private static async Task<float> DrawCompanyHeader(SKCanvas canvas, int width, float y)
	{
		y = ThermalPrintUtil.DrawLogo(canvas, width, y);

		var primaryCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, int.Parse(primaryCompanyId.Value));

		if (company is not null)
			y = ThermalPrintUtil.DrawCompanyHeader(canvas, company, width, y);
		else
		{
			y = ThermalPrintUtil.DrawCenteredText(canvas, "PRIME BAKES", width, y, ThermalPrintUtil.FontSizeTitle, bold: true);
			y += ThermalPrintUtil.SectionGap;
		}

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawBillDetails(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, bill.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, bill.DiningTableId);

		var pairs = new List<(string Label, string Value)>
		{
			("Outlet",  location?.Name ?? "N/A"),
			("Bill No", bill.TransactionNo),
			("Date",    bill.TransactionDateTime.ToString("dd/MMM/yy hh:mm tt"))
		};

		y = ThermalPrintUtil.DrawLabelValueBlock(canvas, pairs, width, y);

		// Table and Pax on the same line
		y = ThermalPrintUtil.DrawSplitRow(canvas,
			"Table", table?.Name ?? "N/A",
			"Pax", bill.TotalPeople.ToString(),
			width, y);

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawItemDetails(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);
		var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		string[] headers = ["Item", "Qty", "Rate", "Amt"];
		SKTextAlign[] alignments = [SKTextAlign.Left, SKTextAlign.Center, SKTextAlign.Right, SKTextAlign.Right];
		float[] columnPercents = [0.44f, 0.14f, 0.20f, 0.22f];

		var rows = new List<string[]>();
		foreach (var item in billDetails)
		{
			string productName = products.FirstOrDefault(p => p.Id == item.ProductId)?.Name ?? "Unknown";
			var hasExtraTax = !item.InclusiveTax && item.TotalTaxAmount > 0;
			if (hasExtraTax)
				productName += " *";

			rows.Add(
			[
				productName,
				item.Quantity.FormatSmartDecimal(),
				item.Rate.FormatSmartDecimal(),
				item.BaseTotal.FormatSmartDecimal()
			]);
		}

		y = ThermalPrintUtil.DrawTable(canvas, headers, alignments, columnPercents, rows, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawTotalDetails(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);

		float[] columnPercents = [0.44f, 0.14f, 0.20f, 0.22f];
		SKTextAlign[] alignments = [SKTextAlign.Left, SKTextAlign.Center, SKTextAlign.Right, SKTextAlign.Right];

		var detailRows = new List<(string Label, string Value)>();

		if (bill.DiscountPercent > 0)
			detailRows.Add(($"Discount ({bill.DiscountPercent}%)", $"- {bill.DiscountAmount.FormatDecimalWithTwoDigits()}"));

		if (bill.ServiceChargePercent > 0)
			detailRows.Add(($"Service ({bill.ServiceChargePercent}%)", bill.ServiceChargeAmount.FormatDecimalWithTwoDigits()));

		decimal cgst = billDetails.Where(s => !s.InclusiveTax).Sum(s => s.CGSTAmount);
		if (cgst > 0)
			detailRows.Add(("CGST", cgst.FormatDecimalWithTwoDigits()));

		decimal sgst = billDetails.Where(s => !s.InclusiveTax).Sum(s => s.SGSTAmount);
		if (sgst > 0)
			detailRows.Add(("SGST", sgst.FormatDecimalWithTwoDigits()));

		decimal igst = billDetails.Where(s => !s.InclusiveTax).Sum(s => s.IGSTAmount);
		if (igst > 0)
			detailRows.Add(("IGST", igst.FormatDecimalWithTwoDigits()));

		if (bill.RoundOffAmount != 0)
			detailRows.Add(("Round Off", bill.RoundOffAmount.FormatDecimalWithTwoDigits()));

		var totalQty = billDetails.Sum(s => s.Quantity);

		y = ThermalPrintUtil.DrawTableTotals(
			canvas, columnPercents, alignments, width, y,
			leftLabel: "Total Qty:",
			columnValue: totalQty.FormatSmartDecimal(),
			columnIndex: 1,
			rightPair: ("Sub Total", bill.TotalAfterTax.FormatDecimalWithTwoDigits()),
			additionalRightRows: detailRows);

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

		y = ThermalPrintUtil.DrawRightLabelValue(canvas,
			"Grand Total", bill.TotalAmount.FormatIndianCurrency(),
			width, y, ThermalPrintUtil.FontSizeHeader);

		CurrencyWordsConverter numericWords = new(new()
		{
			Culture = Culture.Hindi,
			OutputFormat = OutputFormat.English
		});

		string amountInWords = numericWords.ToWords(Math.Round(bill.TotalAmount));
		if (string.IsNullOrEmpty(amountInWords))
			amountInWords = "Zero";
		amountInWords += " Rupees Only";

		y += ThermalPrintUtil.LineGap;
		y = ThermalPrintUtil.DrawCenteredText(canvas, amountInWords, width, y, ThermalPrintUtil.FontSizeSmall, bold: false);

		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static float DrawPaymentModes(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var paymentPairs = new List<(string Label, string Value)>();

		if (bill.Cash > 0)
			paymentPairs.Add(("Cash", bill.Cash.FormatIndianCurrency()));
		if (bill.Card > 0)
			paymentPairs.Add(("Card", bill.Card.FormatIndianCurrency()));
		if (bill.UPI > 0)
			paymentPairs.Add(("UPI", bill.UPI.FormatIndianCurrency()));
		if (bill.Credit > 0)
			paymentPairs.Add(("Credit", bill.Credit.FormatIndianCurrency()));

		if (paymentPairs.Count == 0)
			return y;

		y = ThermalPrintUtil.DrawAlignedBlock(canvas, paymentPairs, width, y);
		y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
		return y;
	}

	private static async Task<float> DrawFooter(SKCanvas canvas, BillModel bill, int width, float y)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		string printedBy = users.FirstOrDefault(u => u.Id == bill.CreatedBy)?.Name ?? "Unknown";
		y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed By: {printedBy} | On: {currentDateTime:dd/MMM/yy hh:mm tt}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, "Thanks. Visit Again", width, y, ThermalPrintUtil.FontSizeNormal, bold: false);
		y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);
		return y;
	}
}
