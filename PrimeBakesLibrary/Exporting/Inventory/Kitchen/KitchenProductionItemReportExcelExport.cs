using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionItemReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        KitchenModel kitchen = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenProductionItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks)] = new() { DisplayName = "Kitchen Production Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenProductionItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(KitchenProductionItemOverviewModel.ItemName),
                nameof(KitchenProductionItemOverviewModel.ItemCode),
                nameof(KitchenProductionItemOverviewModel.ItemCategoryName),
                nameof(KitchenProductionItemOverviewModel.Quantity),
                nameof(KitchenProductionItemOverviewModel.Total)
            ];
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenProductionItemOverviewModel.ItemName),
                nameof(KitchenProductionItemOverviewModel.ItemCode),
                nameof(KitchenProductionItemOverviewModel.ItemCategoryName),
                nameof(KitchenProductionItemOverviewModel.TransactionNo),
                nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
                nameof(KitchenProductionItemOverviewModel.CompanyName),
                nameof(KitchenProductionItemOverviewModel.KitchenName),
                nameof(KitchenProductionItemOverviewModel.Quantity),
                nameof(KitchenProductionItemOverviewModel.Rate),
                nameof(KitchenProductionItemOverviewModel.Total),
                nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks),
                nameof(KitchenProductionItemOverviewModel.Remarks)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.KitchenName));

            if (company is not null)
                columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(KitchenProductionItemOverviewModel.ItemName),
                nameof(KitchenProductionItemOverviewModel.ItemCode),
                nameof(KitchenProductionItemOverviewModel.TransactionNo),
                nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
                nameof(KitchenProductionItemOverviewModel.KitchenName),
                nameof(KitchenProductionItemOverviewModel.Quantity),
                nameof(KitchenProductionItemOverviewModel.Rate),
                nameof(KitchenProductionItemOverviewModel.Total)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.KitchenName));
        }

        var stream = await ExcelReportExportUtil.ExportToExcel(
            kitchenProductionItemData,
            "KITCHEN PRODUCTION ITEM REPORT",
            "Kitchen Production Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_PRODUCTION_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
