using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnItemReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<PurchaseReturnItemOverviewModel> purchaseReturnItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        LedgerModel party = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(PurchaseReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks)] = new() { DisplayName = "Purchase Return Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetTotal)
            ];
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.CompanyName),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.Rate),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountPercent),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.InclusiveTax),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.NetTotal),
                nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks),
                nameof(PurchaseReturnItemOverviewModel.Remarks)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.PartyName));

            if (company is not null)
                columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.NetTotal)
            ];

            if (party is not null)
                columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.PartyName));
        }

        var stream = await ExcelReportExportUtil.ExportToExcel(
            purchaseReturnItemData,
            "PURCHASE RETURN ITEM REPORT",
            "Purchase Return Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = $"PURCHASE_RETURN_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
