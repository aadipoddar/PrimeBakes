using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReturnReportPdfExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<SaleReturnOverviewModel> saleReturnData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null,
        LocationModel location = null)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            // Customize specific columns for PDF display (matching Excel column names)
            [nameof(SaleReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CustomerName)] = new() { DisplayName = "Customer", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.TotalItems)] = new()
            {
                DisplayName = "Items",
                Format = "#,##0",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalQuantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.BaseTotal)] = new()
            {
                DisplayName = "Base Total",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.ItemDiscountAmount)] = new()
            {
                DisplayName = "Item Discount Amount",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalAfterItemDiscount)] = new()
            {
                DisplayName = "After Disc",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount)] = new()
            {
                DisplayName = "Incl Tax",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalExtraTaxAmount)] = new()
            {
                DisplayName = "Extra Tax",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalAfterTax)] = new()
            {
                DisplayName = "Sub Total",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.OtherChargesPercent)] = new()
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

            [nameof(SaleReturnOverviewModel.OtherChargesAmount)] = new()
            {
                DisplayName = "Other Charges",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.DiscountPercent)] = new()
            {
                DisplayName = "Disc %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnOverviewModel.DiscountAmount)] = new()
            {
                DisplayName = "Disc Amt",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.RoundOffAmount)] = new()
            {
                DisplayName = "Round Off",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.TotalAmount)] = new()
            {
                DisplayName = "Total",
                Format = "#,##0.00",
                IsRequired = true,
                IsGrandTotal = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.Cash)] = new()
            {
                DisplayName = "Cash",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.Card)] = new()
            {
                DisplayName = "Card",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.UPI)] = new()
            {
                DisplayName = "UPI",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.Credit)] = new()
            {
                DisplayName = "Credit",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnOverviewModel.PaymentModes)] = new()
            {
                DisplayName = "Payment Modes",
                IncludeInTotal = false
            }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        // Summary view - grouped by party with totals
        if (showSummary)
            columnOrder =
            [
                nameof(SaleReturnOverviewModel.PartyName),
                nameof(SaleReturnOverviewModel.TotalItems),
                nameof(SaleReturnOverviewModel.TotalQuantity),
                nameof(SaleReturnOverviewModel.BaseTotal),
                nameof(SaleReturnOverviewModel.ItemDiscountAmount),
                nameof(SaleReturnOverviewModel.TotalAfterItemDiscount),
                nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleReturnOverviewModel.TotalExtraTaxAmount),
                nameof(SaleReturnOverviewModel.TotalAfterTax),
                nameof(SaleReturnOverviewModel.OtherChargesAmount),
                nameof(SaleReturnOverviewModel.DiscountAmount),
                nameof(SaleReturnOverviewModel.RoundOffAmount),
                nameof(SaleReturnOverviewModel.TotalAmount),
                nameof(SaleReturnOverviewModel.Cash),
                nameof(SaleReturnOverviewModel.Card),
                nameof(SaleReturnOverviewModel.UPI),
                nameof(SaleReturnOverviewModel.Credit)
            ];

        else if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                nameof(SaleReturnOverviewModel.TransactionNo),
                nameof(SaleReturnOverviewModel.CompanyName),
                nameof(SaleReturnOverviewModel.LocationName),
                nameof(SaleReturnOverviewModel.PartyName),
                nameof(SaleReturnOverviewModel.CustomerName),
                nameof(SaleReturnOverviewModel.TransactionDateTime),
                nameof(SaleReturnOverviewModel.FinancialYear),
                nameof(SaleReturnOverviewModel.TotalItems),
                nameof(SaleReturnOverviewModel.TotalQuantity),
                nameof(SaleReturnOverviewModel.BaseTotal),
                nameof(SaleReturnOverviewModel.ItemDiscountAmount),
                nameof(SaleReturnOverviewModel.TotalAfterItemDiscount),
                nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleReturnOverviewModel.TotalExtraTaxAmount),
                nameof(SaleReturnOverviewModel.TotalAfterTax),
                nameof(SaleReturnOverviewModel.OtherChargesPercent),
                nameof(SaleReturnOverviewModel.OtherChargesAmount),
                nameof(SaleReturnOverviewModel.DiscountPercent),
                nameof(SaleReturnOverviewModel.DiscountAmount),
                nameof(SaleReturnOverviewModel.RoundOffAmount),
                nameof(SaleReturnOverviewModel.TotalAmount),
                nameof(SaleReturnOverviewModel.Cash),
                nameof(SaleReturnOverviewModel.Card),
                nameof(SaleReturnOverviewModel.UPI),
                nameof(SaleReturnOverviewModel.Credit),
                nameof(SaleReturnOverviewModel.PaymentModes),
                nameof(SaleReturnOverviewModel.Remarks),
                nameof(SaleReturnOverviewModel.CreatedByName),
                nameof(SaleReturnOverviewModel.CreatedAt),
                nameof(SaleReturnOverviewModel.CreatedFromPlatform),
                nameof(SaleReturnOverviewModel.LastModifiedByUserName),
                nameof(SaleReturnOverviewModel.LastModifiedAt),
                nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)
            ];
        }
        else
        {
            // Summary columns - key fields only
            columnOrder =
            [
                nameof(SaleReturnOverviewModel.TransactionNo),
                nameof(SaleReturnOverviewModel.LocationName),
                nameof(SaleReturnOverviewModel.PartyName),
                nameof(SaleReturnOverviewModel.TransactionDateTime),
                nameof(SaleReturnOverviewModel.TotalQuantity),
                nameof(SaleReturnOverviewModel.TotalAfterTax),
                nameof(SaleReturnOverviewModel.DiscountPercent),
                nameof(SaleReturnOverviewModel.DiscountAmount),
                nameof(SaleReturnOverviewModel.TotalAmount),
                nameof(SaleReturnOverviewModel.PaymentModes)
            ];
        }

        if (company is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.CompanyName));

        if (location is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.LocationName));

        if (party is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.PartyName));

        var stream = await PDFReportExportUtil.ExportToPdf(
            saleReturnData,
            "SALE RETURN REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns || showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_RETURN_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
