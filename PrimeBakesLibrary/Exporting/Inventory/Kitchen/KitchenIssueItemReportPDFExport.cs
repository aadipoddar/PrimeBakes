using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenIssueItemReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        KitchenModel kitchen = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenIssueItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks)] = new() { DisplayName = "Kitchen Issue Remarks", IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false },

            [nameof(KitchenIssueItemOverviewModel.Quantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenIssueItemOverviewModel.Rate)] = new()
            {
                DisplayName = "Rate",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(KitchenIssueItemOverviewModel.Total)] = new()
            {
                DisplayName = "Total",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            }
        };

        List<string> columnOrder;

        if (showSummary)
        {
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Total)
            ];
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
                nameof(KitchenIssueItemOverviewModel.TransactionNo),
                nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
                nameof(KitchenIssueItemOverviewModel.CompanyName),
                nameof(KitchenIssueItemOverviewModel.KitchenName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Rate),
                nameof(KitchenIssueItemOverviewModel.Total),
                nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks),
                nameof(KitchenIssueItemOverviewModel.Remarks)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.KitchenName));

            if (company is not null)
                columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.TransactionNo),
                nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
                nameof(KitchenIssueItemOverviewModel.KitchenName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Rate),
                nameof(KitchenIssueItemOverviewModel.Total)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.KitchenName));
        }

        var stream = await PDFReportExportUtil.ExportToPdf(
            kitchenIssueItemData,
            "KITCHEN ISSUE ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns && !showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_ISSUE_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
