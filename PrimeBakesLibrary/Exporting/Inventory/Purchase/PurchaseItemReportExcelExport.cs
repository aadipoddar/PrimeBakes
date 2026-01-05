using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseItemReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(PurchaseItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.PurchaseRemarks)] = new() { DisplayName = "Purchase Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseItemOverviewModel.ItemName),
                nameof(PurchaseItemOverviewModel.ItemCode),
                nameof(PurchaseItemOverviewModel.ItemCategoryName),
                nameof(PurchaseItemOverviewModel.Quantity),
                nameof(PurchaseItemOverviewModel.BaseTotal),
                nameof(PurchaseItemOverviewModel.DiscountAmount),
                nameof(PurchaseItemOverviewModel.AfterDiscount),
                nameof(PurchaseItemOverviewModel.SGSTAmount),
                nameof(PurchaseItemOverviewModel.CGSTAmount),
                nameof(PurchaseItemOverviewModel.IGSTAmount),
                nameof(PurchaseItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseItemOverviewModel.Total),
                nameof(PurchaseItemOverviewModel.NetTotal)
            ];
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(PurchaseItemOverviewModel.ItemName),
                nameof(PurchaseItemOverviewModel.ItemCode),
                nameof(PurchaseItemOverviewModel.ItemCategoryName),
                nameof(PurchaseItemOverviewModel.TransactionNo),
                nameof(PurchaseItemOverviewModel.TransactionDateTime),
                nameof(PurchaseItemOverviewModel.CompanyName),
                nameof(PurchaseItemOverviewModel.PartyName),
                nameof(PurchaseItemOverviewModel.Quantity),
                nameof(PurchaseItemOverviewModel.Rate),
                nameof(PurchaseItemOverviewModel.BaseTotal),
                nameof(PurchaseItemOverviewModel.DiscountPercent),
                nameof(PurchaseItemOverviewModel.DiscountAmount),
                nameof(PurchaseItemOverviewModel.AfterDiscount),
                nameof(PurchaseItemOverviewModel.SGSTPercent),
                nameof(PurchaseItemOverviewModel.SGSTAmount),
                nameof(PurchaseItemOverviewModel.CGSTPercent),
                nameof(PurchaseItemOverviewModel.CGSTAmount),
                nameof(PurchaseItemOverviewModel.IGSTPercent),
                nameof(PurchaseItemOverviewModel.IGSTAmount),
                nameof(PurchaseItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseItemOverviewModel.InclusiveTax),
                nameof(PurchaseItemOverviewModel.Total),
                nameof(PurchaseItemOverviewModel.NetRate),
                nameof(PurchaseItemOverviewModel.NetTotal),
                nameof(PurchaseItemOverviewModel.PurchaseRemarks),
                nameof(PurchaseItemOverviewModel.Remarks)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseItemOverviewModel.PartyName));

            if (company is not null)
                columnOrder.Remove(nameof(PurchaseItemOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(PurchaseItemOverviewModel.ItemName),
                nameof(PurchaseItemOverviewModel.ItemCode),
                nameof(PurchaseItemOverviewModel.TransactionNo),
                nameof(PurchaseItemOverviewModel.TransactionDateTime),
                nameof(PurchaseItemOverviewModel.PartyName),
                nameof(PurchaseItemOverviewModel.Quantity),
                nameof(PurchaseItemOverviewModel.NetRate),
                nameof(PurchaseItemOverviewModel.NetTotal)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseItemOverviewModel.PartyName));
        }

        var stream = await ExcelReportExportUtil.ExportToExcel(
            purchaseItemData,
            "PURCHASE ITEM REPORT",
            "Purchase Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = $"PURCHASE_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
