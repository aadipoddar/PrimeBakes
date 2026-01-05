using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReturnItemReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<SaleReturnItemOverviewModel> saleReturnItemData,
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
            [nameof(SaleReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.SaleReturnRemarks)] = new() { DisplayName = "Sale Return Remarks", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false },

            [nameof(SaleReturnItemOverviewModel.Quantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.Rate)] = new()
            {
                DisplayName = "Rate",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnItemOverviewModel.NetRate)] = new()
            {
                DisplayName = "Net Rate",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnItemOverviewModel.BaseTotal)] = new()
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

            [nameof(SaleReturnItemOverviewModel.DiscountPercent)] = new()
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

            [nameof(SaleReturnItemOverviewModel.DiscountAmount)] = new()
            {
                DisplayName = "Disc Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.AfterDiscount)] = new()
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

            [nameof(SaleReturnItemOverviewModel.SGSTPercent)] = new()
            {
                DisplayName = "SGST %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnItemOverviewModel.SGSTAmount)] = new()
            {
                DisplayName = "SGST Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.CGSTPercent)] = new()
            {
                DisplayName = "CGST %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnItemOverviewModel.CGSTAmount)] = new()
            {
                DisplayName = "CGST Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.IGSTPercent)] = new()
            {
                DisplayName = "IGST %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(SaleReturnItemOverviewModel.IGSTAmount)] = new()
            {
                DisplayName = "IGST Amt",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.TotalTaxAmount)] = new()
            {
                DisplayName = "Tax",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.Total)] = new()
            {
                DisplayName = "Total",
                Format = "#,##0.00",
                HighlightNegative = true,
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(SaleReturnItemOverviewModel.NetTotal)] = new()
            {
                DisplayName = "Net Total",
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
            columnOrder =
            [
                nameof(SaleReturnItemOverviewModel.ItemName),
                nameof(SaleReturnItemOverviewModel.ItemCode),
                nameof(SaleReturnItemOverviewModel.ItemCategoryName),
                nameof(SaleReturnItemOverviewModel.Quantity),
                nameof(SaleReturnItemOverviewModel.BaseTotal),
                nameof(SaleReturnItemOverviewModel.DiscountAmount),
                nameof(SaleReturnItemOverviewModel.AfterDiscount),
                nameof(SaleReturnItemOverviewModel.SGSTAmount),
                nameof(SaleReturnItemOverviewModel.CGSTAmount),
                nameof(SaleReturnItemOverviewModel.IGSTAmount),
                nameof(SaleReturnItemOverviewModel.TotalTaxAmount),
                nameof(SaleReturnItemOverviewModel.Total),
                nameof(SaleReturnItemOverviewModel.NetTotal)
            ];

        else if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
            List<string> columns =
            [
                nameof(SaleReturnItemOverviewModel.ItemName),
                nameof(SaleReturnItemOverviewModel.ItemCode),
                nameof(SaleReturnItemOverviewModel.ItemCategoryName),
                nameof(SaleReturnItemOverviewModel.TransactionNo),
                nameof(SaleReturnItemOverviewModel.TransactionDateTime),
                nameof(SaleReturnItemOverviewModel.CompanyName)
            ];

            if (location is null)
                columns.Add(nameof(SaleReturnItemOverviewModel.LocationName));

            columns.AddRange([
                nameof(SaleReturnItemOverviewModel.PartyName),
                nameof(SaleReturnItemOverviewModel.Quantity),
                nameof(SaleReturnItemOverviewModel.Rate),
                nameof(SaleReturnItemOverviewModel.BaseTotal),
                nameof(SaleReturnItemOverviewModel.DiscountPercent),
                nameof(SaleReturnItemOverviewModel.DiscountAmount),
                nameof(SaleReturnItemOverviewModel.AfterDiscount),
                nameof(SaleReturnItemOverviewModel.SGSTPercent),
                nameof(SaleReturnItemOverviewModel.SGSTAmount),
                nameof(SaleReturnItemOverviewModel.CGSTPercent),
                nameof(SaleReturnItemOverviewModel.CGSTAmount),
                nameof(SaleReturnItemOverviewModel.IGSTPercent),
                nameof(SaleReturnItemOverviewModel.IGSTAmount),
                nameof(SaleReturnItemOverviewModel.TotalTaxAmount),
                nameof(SaleReturnItemOverviewModel.InclusiveTax),
                nameof(SaleReturnItemOverviewModel.Total),
                nameof(SaleReturnItemOverviewModel.NetRate),
                nameof(SaleReturnItemOverviewModel.NetTotal),
                nameof(SaleReturnItemOverviewModel.SaleReturnRemarks),
                nameof(SaleReturnItemOverviewModel.Remarks)
            ]);

            columnOrder = columns;
        }
        // Summary columns - key fields only (matching Excel export)
        else
            columnOrder =
            [
                nameof(SaleReturnItemOverviewModel.ItemName),
                nameof(SaleReturnItemOverviewModel.ItemCode),
                nameof(SaleReturnItemOverviewModel.TransactionNo),
                nameof(SaleReturnItemOverviewModel.TransactionDateTime),
                nameof(SaleReturnItemOverviewModel.LocationName),
                nameof(SaleReturnItemOverviewModel.PartyName),
                nameof(SaleReturnItemOverviewModel.Quantity),
                nameof(SaleReturnItemOverviewModel.NetRate),
                nameof(SaleReturnItemOverviewModel.NetTotal)
            ];

        var stream = await PDFReportExportUtil.ExportToPdf(
            saleReturnItemData,
            "SALE RETURN ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns || showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_RETURN_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
