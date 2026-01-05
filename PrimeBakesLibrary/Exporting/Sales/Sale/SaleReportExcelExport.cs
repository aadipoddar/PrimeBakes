using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReportExcelExport
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.OrderDateTime)] = new() { DisplayName = "Order Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            [nameof(SaleOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(SaleOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true },
            [nameof(SaleOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Percentage fields - Center aligned, no totals
            [nameof(SaleOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

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
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.CompanyName),
                nameof(SaleOverviewModel.LocationName),
                nameof(SaleOverviewModel.PartyName),
                nameof(SaleOverviewModel.CustomerName),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.OrderDateTime),
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
            ];
        }
        else
        {
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.LocationName),
                nameof(SaleOverviewModel.PartyName),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.DiscountPercent),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.PaymentModes)
            ];
        }

        if (company is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.CompanyName));

        if (location is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.LocationName));

        if (party is not null)
            columnOrder.Remove(nameof(SaleOverviewModel.PartyName));

        var stream = await ExcelReportExportUtil.ExportToExcel(
            saleData,
            "SALE REPORT",
            "Sale Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
