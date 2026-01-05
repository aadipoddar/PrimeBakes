using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenIssueReportPDFExport
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
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenIssueOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },

            [nameof(KitchenIssueOverviewModel.TotalItems)] = new()
            {
                DisplayName = "Items",
                Format = "#,##0",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenIssueOverviewModel.TotalQuantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenIssueOverviewModel.TotalAmount)] = new()
            {
                DisplayName = "Total",
                Format = "#,##0.00",
                HighlightNegative = true,
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

        var stream = await PDFReportExportUtil.ExportToPdf(
            kitchenIssueData,
            "KITCHEN ISSUE REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns && !showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_ISSUE_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
