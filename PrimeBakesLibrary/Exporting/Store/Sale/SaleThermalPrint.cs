using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

using SkiaSharp;

namespace PrimeBakesLibrary.Exporting.Store.Sale;

/// <summary>
/// Renders a store sale thermal receipt as a SkiaSharp raster bitmap and converts it
/// to ESC/POS byte data ready for Bluetooth thermal printing.
/// Uses <see cref="ThermalPrintUtil"/> drawing helpers for consistent visual rendering.
/// </summary>
public static class SaleThermalPrint
{
    /// <summary>
    /// Generates ESC/POS raster bytes for a complete sale thermal receipt.
    /// </summary>
    /// <param name="saleId">The sale transaction ID.</param>
    /// <returns>ESC/POS byte array including initialise, raster image, feed, and cut commands.</returns>
    public static async Task<byte[]> GenerateThermalBill(int saleId)
    {
        using var bitmap = await RenderReceipt(saleId);
        return ThermalPrintUtil.BitmapToEscPosBytes(bitmap);
    }

    /// <summary>
    /// Generates PNG image bytes for a complete sale thermal receipt.
    /// Used as a browser-print fallback when Bluetooth is disconnected.
    /// </summary>
    /// <param name="saleId">The sale transaction ID.</param>
    /// <returns>PNG-encoded byte array of the rendered receipt.</returns>
    public static async Task<byte[]> GenerateThermalBillPng(int saleId)
    {
        using var bitmap = await RenderReceipt(saleId);
        return ThermalPrintUtil.BitmapToPngBytes(bitmap);
    }

    /// <summary>Renders the full sale receipt onto an <see cref="SKBitmap"/>.</summary>
    private static async Task<SKBitmap> RenderReceipt(int saleId)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);

        int width = ThermalPrintUtil.PaperDots80mm;
        int maxHeight = 3000;
        using var tempBitmap = new SKBitmap(width, maxHeight);
        using var canvas = new SKCanvas(tempBitmap);
        canvas.Clear(SKColors.White);

        float y = ThermalPrintUtil.Margin;

        y = await DrawCompanyHeader(canvas, width, y);
        y = await DrawBillDetails(canvas, sale, width, y);
        y = await DrawItemDetails(canvas, sale, width, y);
        y = await DrawTotalDetails(canvas, sale, width, y);
        y = DrawPaymentModes(canvas, sale, width, y);
        y = await DrawFooter(canvas, sale, width, y);

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

    private static async Task<float> DrawBillDetails(SKCanvas canvas, SaleOverviewModel sale, int width, float y)
    {
        var pairs = new List<(string Label, string Value)>
        {
            ("Outlet", sale.LocationName),
            ("Bill No", sale.TransactionNo),
            ("Date", sale.TransactionDateTime.ToString("dd/MMM/yy hh:mm tt"))
        };

        if (sale.OrderId.HasValue && sale.OrderId.Value > 0)
        {
            pairs.Add(("Order", sale.OrderTransactionNo ?? "N/A"));
            if (sale.OrderDateTime.HasValue)
                pairs.Add(("Order Date", sale.OrderDateTime.Value.ToString("dd/MM/yy hh:mm tt")));
        }

        if (sale.PartyId.HasValue && sale.PartyId.Value > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
            if (party is not null)
                pairs.Add(("Party", party.Name));
        }

        if (sale.CustomerId.HasValue && sale.CustomerId.Value > 0)
        {
            var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, sale.CustomerId.Value);
            if (customer is not null)
            {
                pairs.Add(("Cust. Name", customer.Name));
                pairs.Add(("Cust. No.", customer.Number));
            }
        }

        y = ThermalPrintUtil.DrawLabelValueBlock(canvas, pairs, width, y);

        y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

        return y;
    }

    private static async Task<float> DrawItemDetails(SKCanvas canvas, SaleOverviewModel sale, int width, float y)
    {
        var saleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);
        var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        string[] headers = ["Item", "Qty", "Rate", "Amt"];
        SKTextAlign[] alignments = [SKTextAlign.Left, SKTextAlign.Center, SKTextAlign.Right, SKTextAlign.Right];
        float[] columnPercents = [0.44f, 0.14f, 0.20f, 0.22f];

        var rows = new List<string[]>();
        foreach (var item in saleDetails)
        {
            string productName = products.FirstOrDefault(p => p.Id == item.ProductId)?.Name ?? "Unknown";
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

    /// <summary>
    /// Draws total qty + sub-total, tax breakdown, discount, round-off, grand total, and amount in words.
    /// Uses the same column layout as the items table so the qty value sits under the Qty column.
    /// </summary>
    private static async Task<float> DrawTotalDetails(SKCanvas canvas, SaleOverviewModel sale, int width, float y)
    {
        var saleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);

        // Must match the column layout in DrawItemDetails
        float[] columnPercents = [0.44f, 0.14f, 0.20f, 0.22f];
        SKTextAlign[] alignments = [SKTextAlign.Left, SKTextAlign.Center, SKTextAlign.Right, SKTextAlign.Right];

        // Build right-aligned detail rows as (label, value) tuples
        var detailRows = new List<(string Label, string Value)>();

        if (sale.DiscountPercent > 0)
            detailRows.Add(($"Discount ({sale.DiscountPercent}%)", $"- {sale.DiscountAmount.FormatDecimalWithTwoDigits()}"));

        decimal cgst = saleDetails.Where(s => !s.InclusiveTax).Sum(s => s.CGSTAmount);
        if (cgst > 0)
            detailRows.Add(("CGST", cgst.FormatDecimalWithTwoDigits()));

        decimal sgst = saleDetails.Where(s => !s.InclusiveTax).Sum(s => s.SGSTAmount);
        if (sgst > 0)
            detailRows.Add(("SGST", sgst.FormatDecimalWithTwoDigits()));

        decimal igst = saleDetails.Where(s => !s.InclusiveTax).Sum(s => s.IGSTAmount);
        if (igst > 0)
            detailRows.Add(("IGST", igst.FormatDecimalWithTwoDigits()));

        if (sale.RoundOffAmount != 0)
            detailRows.Add(("Round Off", sale.RoundOffAmount.FormatDecimalWithTwoDigits()));

        y = ThermalPrintUtil.DrawTableTotals(
            canvas, columnPercents, alignments, width, y,
            leftLabel: "Total Qty:",
            columnValue: sale.TotalQuantity.FormatSmartDecimal(),
            columnIndex: 1,  // Qty column
            rightPair: ("Sub Total", sale.TotalAfterTax.FormatDecimalWithTwoDigits()),
            additionalRightRows: detailRows);

        // Separator before grand total
        y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

        // Grand total (bold, larger font, right-aligned with label and value close together)
        y = ThermalPrintUtil.DrawRightLabelValue(canvas,
            "Grand Total", sale.TotalAmount.FormatIndianCurrency(),
            width, y, ThermalPrintUtil.FontSizeHeader);

        // Amount in words
        CurrencyWordsConverter numericWords = new(new()
        {
            Culture = Culture.Hindi,
            OutputFormat = OutputFormat.English
        });

        string amountInWords = numericWords.ToWords(Math.Round(sale.TotalAmount));
        if (string.IsNullOrEmpty(amountInWords))
            amountInWords = "Zero";

        amountInWords += " Rupees Only";

        y += ThermalPrintUtil.LineGap;
        y = ThermalPrintUtil.DrawCenteredText(canvas, amountInWords, width, y, ThermalPrintUtil.FontSizeSmall, bold: false);

        y = ThermalPrintUtil.DrawSeparator(canvas, width, y);

        return y;
    }

    private static float DrawPaymentModes(SKCanvas canvas, SaleOverviewModel sale, int width, float y)
    {
        var paymentPairs = new List<(string Label, string Value)>();

        if (sale.Cash > 0)
            paymentPairs.Add(("Cash", sale.Cash.FormatIndianCurrency()));

        if (sale.Card > 0)
            paymentPairs.Add(("Card", sale.Card.FormatIndianCurrency()));

        if (sale.UPI > 0)
            paymentPairs.Add(("UPI", sale.UPI.FormatIndianCurrency()));

        if (sale.Credit > 0)
            paymentPairs.Add(("Credit", sale.Credit.FormatIndianCurrency()));

        if (paymentPairs.Count == 0)
            return y;

        y = ThermalPrintUtil.DrawAlignedBlock(canvas, paymentPairs, width, y);
        y = ThermalPrintUtil.DrawSeparator(canvas, width, y);
        return y;
    }

    private static async Task<float> DrawFooter(SKCanvas canvas, SaleOverviewModel sale, int width, float y)
    {
        var currentDateTime = await CommonData.LoadCurrentDateTime();
        y = ThermalPrintUtil.DrawCenteredText(canvas, $"Printed By: {sale.CreatedByName} | On: {currentDateTime:dd/MMM/yy hh:mm tt}", width, y, ThermalPrintUtil.FontSizeSmall, bold: false);
        y = ThermalPrintUtil.DrawCenteredText(canvas, "Thanks. Visit Again", width, y, ThermalPrintUtil.FontSizeNormal, bold: false);
        y = ThermalPrintUtil.DrawCenteredText(canvas, "A Product of aadisoft.vercel.app", width, y, ThermalPrintUtil.FontSizeSmall, bold: true);

        return y;
    }
}
