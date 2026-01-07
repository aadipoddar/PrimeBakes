using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnReportExcelExport
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        List<string> columnOrder;

        // Summary view - grouped by party with totals
        if (showSummary)
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

        var stream = await ExcelReportExportUtil.ExportToExcel(
            purchaseReturnData,
            "PURCHASE RETURN REPORT",
            "Purchase Return Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = $"PURCHASE_RETURN_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
