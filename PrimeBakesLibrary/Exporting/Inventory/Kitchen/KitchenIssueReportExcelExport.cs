using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenIssueReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<KitchenIssueOverviewModel> kitchenIssueData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        KitchenModel kitchen = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenIssueOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenIssueOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenIssueOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.KitchenName),
                nameof(KitchenIssueOverviewModel.TotalItems),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenIssueOverviewModel.KitchenName));
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.TransactionNo),
                nameof(KitchenIssueOverviewModel.TransactionDateTime),
                nameof(KitchenIssueOverviewModel.CompanyName),
                nameof(KitchenIssueOverviewModel.KitchenName),
                nameof(KitchenIssueOverviewModel.FinancialYear),
                nameof(KitchenIssueOverviewModel.TotalItems),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount),
                nameof(KitchenIssueOverviewModel.Remarks),
                nameof(KitchenIssueOverviewModel.CreatedByName),
                nameof(KitchenIssueOverviewModel.CreatedAt),
                nameof(KitchenIssueOverviewModel.CreatedFromPlatform),
                nameof(KitchenIssueOverviewModel.LastModifiedByUserName),
                nameof(KitchenIssueOverviewModel.LastModifiedAt),
                nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenIssueOverviewModel.KitchenName));

            if (company is not null)
                columnOrder.Remove(nameof(KitchenIssueOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.TransactionNo),
                nameof(KitchenIssueOverviewModel.TransactionDateTime),
                nameof(KitchenIssueOverviewModel.KitchenName),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenIssueOverviewModel.KitchenName));
        }

        var stream = await ExcelReportExportUtil.ExportToExcel(
            kitchenIssueData,
            "KITCHEN ISSUE REPORT",
            "Kitchen Issue Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_ISSUE_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
