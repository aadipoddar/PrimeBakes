using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleItemReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<SaleItemOverviewModel> saleItemData,
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
            [nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.SaleRemarks)] = new() { DisplayName = "Sale Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Date fields
            [nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Quantity
            [nameof(SaleItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(SaleItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(SaleItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
        if (showSummary)
            columnOrder =
            [
                nameof(SaleItemOverviewModel.ItemName),
                nameof(SaleItemOverviewModel.ItemCode),
                nameof(SaleItemOverviewModel.ItemCategoryName),
                nameof(SaleItemOverviewModel.Quantity),
                nameof(SaleItemOverviewModel.BaseTotal),
                nameof(SaleItemOverviewModel.DiscountAmount),
                nameof(SaleItemOverviewModel.AfterDiscount),
                nameof(SaleItemOverviewModel.SGSTAmount),
                nameof(SaleItemOverviewModel.CGSTAmount),
                nameof(SaleItemOverviewModel.IGSTAmount),
                nameof(SaleItemOverviewModel.TotalTaxAmount),
                nameof(SaleItemOverviewModel.Total),
                nameof(SaleItemOverviewModel.NetTotal)
            ];

        // All columns in logical order
        else if (showAllColumns)
        {
            List<string> columns =
            [
                nameof(SaleItemOverviewModel.ItemName),
                nameof(SaleItemOverviewModel.ItemCode),
                nameof(SaleItemOverviewModel.ItemCategoryName),
                nameof(SaleItemOverviewModel.TransactionNo),
                nameof(SaleItemOverviewModel.TransactionDateTime),
                nameof(SaleItemOverviewModel.CompanyName)
            ];

            if (location is null)
                columns.Add(nameof(SaleItemOverviewModel.LocationName));

            columns.AddRange([
                nameof(SaleItemOverviewModel.PartyName),
                nameof(SaleItemOverviewModel.Quantity),
                nameof(SaleItemOverviewModel.Rate),
                nameof(SaleItemOverviewModel.BaseTotal),
                nameof(SaleItemOverviewModel.DiscountPercent),
                nameof(SaleItemOverviewModel.DiscountAmount),
                nameof(SaleItemOverviewModel.AfterDiscount),
                nameof(SaleItemOverviewModel.SGSTPercent),
                nameof(SaleItemOverviewModel.SGSTAmount),
                nameof(SaleItemOverviewModel.CGSTPercent),
                nameof(SaleItemOverviewModel.CGSTAmount),
                nameof(SaleItemOverviewModel.IGSTPercent),
                nameof(SaleItemOverviewModel.IGSTAmount),
                nameof(SaleItemOverviewModel.TotalTaxAmount),
                nameof(SaleItemOverviewModel.InclusiveTax),
                nameof(SaleItemOverviewModel.Total),
                nameof(SaleItemOverviewModel.NetRate),
                nameof(SaleItemOverviewModel.NetTotal),
                nameof(SaleItemOverviewModel.SaleRemarks),
                nameof(SaleItemOverviewModel.Remarks)
            ]);

            columnOrder = columns;
        }
        // Summary columns only
        else
            columnOrder =
            [
                nameof(SaleItemOverviewModel.ItemName),
                nameof(SaleItemOverviewModel.ItemCode),
                nameof(SaleItemOverviewModel.TransactionNo),
                nameof(SaleItemOverviewModel.TransactionDateTime),
                nameof(SaleItemOverviewModel.LocationName),
                nameof(SaleItemOverviewModel.PartyName),
                nameof(SaleItemOverviewModel.Quantity),
                nameof(SaleItemOverviewModel.NetRate),
                nameof(SaleItemOverviewModel.NetTotal)
            ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            saleItemData,
            "SALE ITEM REPORT",
            "Sale Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
        );

        string fileName = "SALE_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
