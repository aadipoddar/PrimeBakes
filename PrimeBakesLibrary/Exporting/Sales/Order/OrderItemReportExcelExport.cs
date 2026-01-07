using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

public static class OrderItemReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        CompanyModel company = null,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(OrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.OrderRemarks)] = new() { DisplayName = "Order Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(OrderItemOverviewModel.ItemName),
                nameof(OrderItemOverviewModel.ItemCode),
                nameof(OrderItemOverviewModel.ItemCategoryName),
                nameof(OrderItemOverviewModel.Quantity)
            ];
        }
        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(OrderItemOverviewModel.ItemName),
                nameof(OrderItemOverviewModel.ItemCode),
                nameof(OrderItemOverviewModel.ItemCategoryName),
                nameof(OrderItemOverviewModel.TransactionNo),
                nameof(OrderItemOverviewModel.TransactionDateTime),
                nameof(OrderItemOverviewModel.CompanyName),
                nameof(OrderItemOverviewModel.LocationName),
                nameof(OrderItemOverviewModel.SaleTransactionNo),
                nameof(OrderItemOverviewModel.Quantity),
                nameof(OrderItemOverviewModel.OrderRemarks),
                nameof(OrderItemOverviewModel.Remarks)
            ];
        }
        else
        {
            columnOrder =
            [
                nameof(OrderItemOverviewModel.ItemName),
                nameof(OrderItemOverviewModel.ItemCode),
                nameof(OrderItemOverviewModel.TransactionNo),
                nameof(OrderItemOverviewModel.TransactionDateTime),
                nameof(OrderItemOverviewModel.LocationName),
                nameof(OrderItemOverviewModel.SaleTransactionNo),
                nameof(OrderItemOverviewModel.Quantity)
            ];
        }

        if (company is not null)
            columnOrder.Remove(nameof(OrderItemOverviewModel.CompanyName));

        if (location is not null)
            columnOrder.Remove(nameof(OrderItemOverviewModel.LocationName));

        var stream = await ExcelReportExportUtil.ExportToExcel(
            orderItemData,
            "ORDER ITEM REPORT",
            "Order Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
        );

        string fileName = "ORDER_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
