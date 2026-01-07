using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferReportPdfExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<StockTransferOverviewModel> data,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        CompanyModel company = null,
        LocationModel fromLocation = null,
        LocationModel toLocation = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            // Map display names and formats similar to Excel export
            [nameof(StockTransferOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.LocationName)] = new() { DisplayName = "From Location", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },

            [nameof(StockTransferOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", IncludeInTotal = false, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", IncludeInTotal = false, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", IsRequired = true, IsGrandTotal = true, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } },
            [nameof(StockTransferOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", IncludeInTotal = false }
        };

        List<string> columnOrder;

        if (showSummary)
            columnOrder =
            [
                nameof(StockTransferOverviewModel.ToLocationName),
                nameof(StockTransferOverviewModel.TotalItems),
                nameof(StockTransferOverviewModel.TotalQuantity),
                nameof(StockTransferOverviewModel.BaseTotal),
                nameof(StockTransferOverviewModel.ItemDiscountAmount),
                nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
                nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
                nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
                nameof(StockTransferOverviewModel.TotalAfterTax),
                nameof(StockTransferOverviewModel.OtherChargesAmount),
                nameof(StockTransferOverviewModel.DiscountAmount),
                nameof(StockTransferOverviewModel.RoundOffAmount),
                nameof(StockTransferOverviewModel.TotalAmount),
                nameof(StockTransferOverviewModel.Cash),
                nameof(StockTransferOverviewModel.Card),
                nameof(StockTransferOverviewModel.UPI),
                nameof(StockTransferOverviewModel.Credit)
            ];
        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(StockTransferOverviewModel.TransactionNo),
                nameof(StockTransferOverviewModel.CompanyName),
                nameof(StockTransferOverviewModel.LocationName),
                nameof(StockTransferOverviewModel.ToLocationName),
                nameof(StockTransferOverviewModel.TransactionDateTime),
                nameof(StockTransferOverviewModel.FinancialYear),
                nameof(StockTransferOverviewModel.TotalItems),
                nameof(StockTransferOverviewModel.TotalQuantity),
                nameof(StockTransferOverviewModel.BaseTotal),
                nameof(StockTransferOverviewModel.ItemDiscountAmount),
                nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
                nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
                nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
                nameof(StockTransferOverviewModel.TotalAfterTax),
                nameof(StockTransferOverviewModel.OtherChargesPercent),
                nameof(StockTransferOverviewModel.OtherChargesAmount),
                nameof(StockTransferOverviewModel.DiscountPercent),
                nameof(StockTransferOverviewModel.DiscountAmount),
                nameof(StockTransferOverviewModel.RoundOffAmount),
                nameof(StockTransferOverviewModel.TotalAmount),
                nameof(StockTransferOverviewModel.Cash),
                nameof(StockTransferOverviewModel.Card),
                nameof(StockTransferOverviewModel.UPI),
                nameof(StockTransferOverviewModel.Credit),
                nameof(StockTransferOverviewModel.PaymentModes),
                nameof(StockTransferOverviewModel.Remarks),
                nameof(StockTransferOverviewModel.CreatedByName),
                nameof(StockTransferOverviewModel.CreatedAt),
                nameof(StockTransferOverviewModel.CreatedFromPlatform),
                nameof(StockTransferOverviewModel.LastModifiedByUserName),
                nameof(StockTransferOverviewModel.LastModifiedAt),
                nameof(StockTransferOverviewModel.LastModifiedFromPlatform)
            ];
        }
        else
        {
            columnOrder =
            [
                nameof(StockTransferOverviewModel.TransactionNo),
                nameof(StockTransferOverviewModel.LocationName),
                nameof(StockTransferOverviewModel.ToLocationName),
                nameof(StockTransferOverviewModel.TransactionDateTime),
                nameof(StockTransferOverviewModel.TotalQuantity),
                nameof(StockTransferOverviewModel.TotalAfterTax),
                nameof(StockTransferOverviewModel.DiscountPercent),
                nameof(StockTransferOverviewModel.DiscountAmount),
                nameof(StockTransferOverviewModel.TotalAmount),
                nameof(StockTransferOverviewModel.PaymentModes)
            ];
        }

        if (company is not null)
            columnOrder.Remove(nameof(StockTransferOverviewModel.CompanyName));

        if (fromLocation is not null)
            columnOrder.Remove(nameof(StockTransferOverviewModel.LocationName));

        if (toLocation is not null)
            columnOrder.Remove(nameof(StockTransferOverviewModel.ToLocationName));

        var stream = await PDFReportExportUtil.ExportToPdf(
            data,
            "STOCK TRANSFER REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns || showSummary,
            new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
        );

        string fileName = "STOCK_TRANSFER_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
