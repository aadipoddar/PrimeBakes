using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnReportPdfExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<PurchaseReturnOverviewModel> purchaseReturnData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },

            [nameof(PurchaseReturnOverviewModel.TotalItems)] = new()
            {
                DisplayName = "Items",
                Format = "#,##0",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.BaseTotal)] = new()
            {
                DisplayName = "Base Total",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new()
            {
                DisplayName = "Dis Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new()
            {
                DisplayName = "After Disc",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new()
            {
                DisplayName = "Incl Tax",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new()
            {
                DisplayName = "Extra Tax",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new()
            {
                DisplayName = "Sub Total",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new()
            {
                DisplayName = "Other Charges %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new()
            {
                DisplayName = "Other Charges Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new()
            {
                DisplayName = "Cash Disc %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new()
            {
                DisplayName = "Cash Disc Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new()
            {
                DisplayName = "Round Off",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(PurchaseReturnOverviewModel.TotalAmount)] = new()
            {
                DisplayName = "Total",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.TotalItems),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.BaseTotal),
                nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
                nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
                nameof(PurchaseReturnOverviewModel.RoundOffAmount),
                nameof(PurchaseReturnOverviewModel.TotalAmount)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.CompanyName),
                nameof(PurchaseReturnOverviewModel.FinancialYear),
                nameof(PurchaseReturnOverviewModel.TotalItems),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.BaseTotal),
                nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
                nameof(PurchaseReturnOverviewModel.RoundOffAmount),
                nameof(PurchaseReturnOverviewModel.TotalAmount),
                nameof(PurchaseReturnOverviewModel.Remarks),
                nameof(PurchaseReturnOverviewModel.CreatedByName),
                nameof(PurchaseReturnOverviewModel.CreatedAt),
                nameof(PurchaseReturnOverviewModel.CreatedFromPlatform),
                nameof(PurchaseReturnOverviewModel.LastModifiedByUserName),
                nameof(PurchaseReturnOverviewModel.LastModifiedAt),
                nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));

            if (company is not null)
                columnOrder.Remove(nameof(PurchaseReturnOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.TotalAmount)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));
        }

        var stream = await PDFReportExportUtil.ExportToPdf(
            purchaseReturnData,
            "PURCHASE RETURN REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns || showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = $"PURCHASE_RETURN_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
