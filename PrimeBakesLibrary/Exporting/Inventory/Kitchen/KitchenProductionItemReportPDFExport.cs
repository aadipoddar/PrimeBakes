using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionItemReportPDFExport
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
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenProductionItemOverviewModel.ItemName)] = new() { DisplayName = "Product", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks)] = new() { DisplayName = "Kitchen Production Remarks", IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.Remarks)] = new() { DisplayName = "Product Remarks", IncludeInTotal = false },

            [nameof(KitchenProductionItemOverviewModel.Quantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenProductionItemOverviewModel.Rate)] = new()
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

            [nameof(KitchenProductionItemOverviewModel.Total)] = new()
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

        var stream = await PDFReportExportUtil.ExportToPdf(
            kitchenProductionItemData,
            "KITCHEN PRODUCTION ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns && !showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_PRODUCTION_ITEM_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
