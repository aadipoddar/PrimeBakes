using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReturnItemReportExcelExport
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // Text fields
            [nameof(SaleReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.SaleReturnRemarks)] = new() { DisplayName = "Sale Return Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Date fields
            [nameof(SaleReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Quantity
            [nameof(SaleReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(SaleReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(SaleReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(SaleReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
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

        // All columns in logical order
        else if (showAllColumns)
        {
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
        // Summary columns only
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

        var stream = await ExcelReportExportUtil.ExportToExcel(
            saleReturnItemData,
            "SALE RETURN ITEM REPORT",
            "Sale Return Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_RETURN_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
