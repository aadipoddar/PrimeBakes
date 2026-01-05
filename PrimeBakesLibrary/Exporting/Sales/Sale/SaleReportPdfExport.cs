using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReportPdfExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<SaleOverviewModel> saleData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order Trans No", IncludeInTotal = false },
            [nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false },
            [nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
            [nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", IncludeInTotal = false },
            [nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(SaleOverviewModel.OrderDateTime)] = new() { DisplayName = "Order Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },
            [nameof(SaleOverviewModel.TotalItems)] = new()
            {
                DisplayName = "Items",
                Format = "#,##0",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalQuantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.BaseTotal)] = new()
            {
                DisplayName = "Base Total",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.ItemDiscountAmount)] = new()
            {
                DisplayName = "Item Discount Amount",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new()
            {
                DisplayName = "After Disc",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new()
            {
                DisplayName = "Incl Tax",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new()
            {
                DisplayName = "Extra Tax",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalAfterTax)] = new()
            {
                DisplayName = "Sub Total",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.OtherChargesPercent)] = new()
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

            [nameof(SaleOverviewModel.OtherChargesAmount)] = new()
            {
                DisplayName = "Other Charges",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.DiscountPercent)] = new()
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

            [nameof(SaleOverviewModel.DiscountAmount)] = new()
            {
                DisplayName = "Disc Amt",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.RoundOffAmount)] = new()
            {
                DisplayName = "Round Off",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.TotalAmount)] = new()
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

            [nameof(SaleOverviewModel.Cash)] = new()
            {
                DisplayName = "Cash",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.Card)] = new()
            {
                DisplayName = "Card",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.UPI)] = new()
            {
                DisplayName = "UPI",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.Credit)] = new()
            {
                DisplayName = "Credit",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleOverviewModel.PaymentModes)] = new()
            {
                DisplayName = "Payment Modes",
                IncludeInTotal = false
            }
        };

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

        // Summary view - grouped by location with totals
        if (showSummary)
            columnOrder =
            [
                nameof(SaleOverviewModel.LocationName),
                nameof(SaleOverviewModel.TotalItems),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.BaseTotal),
                nameof(SaleOverviewModel.ItemDiscountAmount),
                nameof(SaleOverviewModel.TotalAfterItemDiscount),
                nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleOverviewModel.TotalExtraTaxAmount),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.OtherChargesAmount),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.RoundOffAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.Cash),
                nameof(SaleOverviewModel.Card),
                nameof(SaleOverviewModel.UPI),
                nameof(SaleOverviewModel.Credit)
            ];

        else if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.CompanyName)
            ];

            if (location is null)
                columnOrder.Add(nameof(SaleOverviewModel.LocationName));

            if (party is null)
                columnOrder.Add(nameof(SaleOverviewModel.PartyName));

            // Continue with remaining columns
            columnOrder.AddRange(
            [
                nameof(SaleOverviewModel.CustomerName),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.FinancialYear),
                nameof(SaleOverviewModel.TotalItems),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.BaseTotal),
                nameof(SaleOverviewModel.ItemDiscountAmount),
                nameof(SaleOverviewModel.TotalAfterItemDiscount),
                nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleOverviewModel.TotalExtraTaxAmount),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.OtherChargesPercent),
                nameof(SaleOverviewModel.OtherChargesAmount),
                nameof(SaleOverviewModel.DiscountPercent),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.RoundOffAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.Cash),
                nameof(SaleOverviewModel.Card),
                nameof(SaleOverviewModel.UPI),
                nameof(SaleOverviewModel.Credit),
                nameof(SaleOverviewModel.PaymentModes),
                nameof(SaleOverviewModel.Remarks),
                nameof(SaleOverviewModel.CreatedByName),
                nameof(SaleOverviewModel.CreatedAt),
                nameof(SaleOverviewModel.CreatedFromPlatform),
                nameof(SaleOverviewModel.LastModifiedByUserName),
                nameof(SaleOverviewModel.LastModifiedAt),
                nameof(SaleOverviewModel.LastModifiedFromPlatform)
            ]);
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.DiscountPercent),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.PaymentModes)
            ];

            if (location is null)
                columnOrder.Insert(3, nameof(SaleOverviewModel.LocationName));

            if (party is null)
            {
                int insertIndex = location is null ? 4 : 3;
                columnOrder.Insert(insertIndex, nameof(SaleOverviewModel.PartyName));
            }
        }

        var stream = await PDFReportExportUtil.ExportToPdf(
            saleData,
            "SALE REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns || showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
