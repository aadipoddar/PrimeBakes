using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

public static class OrderReportExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<OrderOverviewModel> orderData,
        ReportExportType exportType,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        CompanyModel company = null,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(OrderOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(OrderOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(OrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(OrderOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
        };

        List<string> columnOrder;

        if (showSummary)
            columnOrder =
            [
                nameof(OrderOverviewModel.LocationName),
                nameof(OrderOverviewModel.TotalItems),
                nameof(OrderOverviewModel.TotalQuantity)
            ];
        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(OrderOverviewModel.TransactionNo),
                nameof(OrderOverviewModel.SaleTransactionNo),
                nameof(OrderOverviewModel.CompanyName),
                nameof(OrderOverviewModel.LocationName),
                nameof(OrderOverviewModel.TransactionDateTime),
                nameof(OrderOverviewModel.SaleDateTime),
                nameof(OrderOverviewModel.FinancialYear),
                nameof(OrderOverviewModel.TotalItems),
                nameof(OrderOverviewModel.TotalQuantity),
                nameof(OrderOverviewModel.Remarks),
                nameof(OrderOverviewModel.CreatedByName),
                nameof(OrderOverviewModel.CreatedAt),
                nameof(OrderOverviewModel.CreatedFromPlatform),
                nameof(OrderOverviewModel.LastModifiedByUserName),
                nameof(OrderOverviewModel.LastModifiedAt),
                nameof(OrderOverviewModel.LastModifiedFromPlatform)
            ];
        }
        else
        {
            columnOrder =
            [
                nameof(OrderOverviewModel.TransactionNo),
                nameof(OrderOverviewModel.SaleTransactionNo),
                nameof(OrderOverviewModel.LocationName),
                nameof(OrderOverviewModel.TransactionDateTime),
                nameof(OrderOverviewModel.TotalItems),
                nameof(OrderOverviewModel.TotalQuantity)
            ];
        }

        if (company is not null)
            columnOrder.Remove(nameof(OrderOverviewModel.CompanyName));

        if (location is not null)
            columnOrder.Remove(nameof(OrderOverviewModel.LocationName));

        string fileName = "ORDER_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                orderData,
                "ORDER REPORT",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                useBuiltInStyle: false,
                useLandscape: showAllColumns && !showSummary,
                new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
            );

            fileName += ".pdf";
            return (stream, fileName);
        }
        else
        {
            var stream = await ExcelReportExportUtil.ExportToExcel(
                orderData,
                "ORDER REPORT",
                "Order Transactions",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }

    public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
        ReportExportType exportType,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        CompanyModel company = null,
        LocationModel location = null)
    {
        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(OrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.OrderRemarks)] = new() { DisplayName = "Order Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
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

        string fileName = "ORDER_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                orderItemData,
                "ORDER ITEM REPORT",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                useBuiltInStyle: false,
                useLandscape: showAllColumns && !showSummary,
                new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
            );

            fileName += ".pdf";
            return (stream, fileName);
        }
        else
        {
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

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }
}
