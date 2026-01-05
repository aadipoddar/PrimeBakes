using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<PurchaseOverviewModel> purchaseData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(PurchaseOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseOverviewModel.PartyName),
                nameof(PurchaseOverviewModel.TotalItems),
                nameof(PurchaseOverviewModel.TotalQuantity),
                nameof(PurchaseOverviewModel.BaseTotal),
                nameof(PurchaseOverviewModel.ItemDiscountAmount),
                nameof(PurchaseOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseOverviewModel.TotalAfterTax),
                nameof(PurchaseOverviewModel.CashDiscountAmount),
                nameof(PurchaseOverviewModel.OtherChargesAmount),
                nameof(PurchaseOverviewModel.RoundOffAmount),
                nameof(PurchaseOverviewModel.TotalAmount)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseOverviewModel.PartyName));
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(PurchaseOverviewModel.TransactionNo),
                nameof(PurchaseOverviewModel.TransactionDateTime),
                nameof(PurchaseOverviewModel.PartyName),
                nameof(PurchaseOverviewModel.CompanyName),
                nameof(PurchaseOverviewModel.FinancialYear),
                nameof(PurchaseOverviewModel.TotalItems),
                nameof(PurchaseOverviewModel.TotalQuantity),
                nameof(PurchaseOverviewModel.BaseTotal),
                nameof(PurchaseOverviewModel.ItemDiscountAmount),
                nameof(PurchaseOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseOverviewModel.TotalAfterTax),
                nameof(PurchaseOverviewModel.OtherChargesPercent),
                nameof(PurchaseOverviewModel.OtherChargesAmount),
                nameof(PurchaseOverviewModel.CashDiscountPercent),
                nameof(PurchaseOverviewModel.CashDiscountAmount),
                nameof(PurchaseOverviewModel.RoundOffAmount),
                nameof(PurchaseOverviewModel.TotalAmount),
                nameof(PurchaseOverviewModel.Remarks),
                nameof(PurchaseOverviewModel.CreatedByName),
                nameof(PurchaseOverviewModel.CreatedAt),
                nameof(PurchaseOverviewModel.CreatedFromPlatform),
                nameof(PurchaseOverviewModel.LastModifiedByUserName),
                nameof(PurchaseOverviewModel.LastModifiedAt),
                nameof(PurchaseOverviewModel.LastModifiedFromPlatform)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseOverviewModel.PartyName));

            if (company is not null)
                columnOrder.Remove(nameof(PurchaseOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(PurchaseOverviewModel.PartyName),
                nameof(PurchaseOverviewModel.TransactionNo),
                nameof(PurchaseOverviewModel.TransactionDateTime),
                nameof(PurchaseOverviewModel.TotalQuantity),
                nameof(PurchaseOverviewModel.TotalAfterTax),
                nameof(PurchaseOverviewModel.OtherChargesPercent),
                nameof(PurchaseOverviewModel.CashDiscountPercent),
                nameof(PurchaseOverviewModel.TotalAmount)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseOverviewModel.PartyName));
        }

        var stream = await ExcelReportExportUtil.ExportToExcel(
            purchaseData,
            "PURCHASE REPORT",
            "Purchase Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = $"PURCHASE_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
