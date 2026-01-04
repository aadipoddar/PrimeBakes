using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReturnReportExcelExport
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // Text fields - Left aligned
            [nameof(SaleReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Dates - Center aligned with custom format
            [nameof(SaleReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            [nameof(SaleReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(SaleReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true },
            [nameof(SaleReturnOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Percentage fields - Center aligned, no totals
            [nameof(SaleReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
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

        var stream = await ExcelReportExportUtil.ExportToExcel(
            saleReturnData,
            "SALE RETURN REPORT",
            "Sale Return Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_RETURN_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
