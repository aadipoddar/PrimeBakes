using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionReportPDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false,
        KitchenModel kitchen = null,
        CompanyModel company = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(KitchenProductionOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },

            [nameof(KitchenProductionOverviewModel.TotalItems)] = new()
            {
                DisplayName = "Items",
                Format = "#,##0",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenProductionOverviewModel.TotalQuantity)] = new()
            {
                DisplayName = "Qty",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                }
            },

            [nameof(KitchenProductionOverviewModel.TotalAmount)] = new()
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
                nameof(KitchenProductionOverviewModel.KitchenName),
                nameof(KitchenProductionOverviewModel.TotalItems),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenProductionOverviewModel.KitchenName));
        }

        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.TransactionNo),
                nameof(KitchenProductionOverviewModel.TransactionDateTime),
                nameof(KitchenProductionOverviewModel.CompanyName),
                nameof(KitchenProductionOverviewModel.KitchenName),
                nameof(KitchenProductionOverviewModel.FinancialYear),
                nameof(KitchenProductionOverviewModel.TotalItems),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount),
                nameof(KitchenProductionOverviewModel.Remarks),
                nameof(KitchenProductionOverviewModel.CreatedByName),
                nameof(KitchenProductionOverviewModel.CreatedAt),
                nameof(KitchenProductionOverviewModel.CreatedFromPlatform),
                nameof(KitchenProductionOverviewModel.LastModifiedByUserName),
                nameof(KitchenProductionOverviewModel.LastModifiedAt),
                nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenProductionOverviewModel.KitchenName));

            if (company is not null)
                columnOrder.Remove(nameof(KitchenProductionOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.TransactionNo),
                nameof(KitchenProductionOverviewModel.TransactionDateTime),
                nameof(KitchenProductionOverviewModel.KitchenName),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount)
            ];

            if (kitchen is not null)
                columnOrder.Remove(nameof(KitchenProductionOverviewModel.KitchenName));
        }

        var stream = await PDFReportExportUtil.ExportToPdf(
            kitchenProductionData,
            "KITCHEN PRODUCTION REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns && !showSummary,
            new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
        );

        string fileName = $"KITCHEN_PRODUCTION_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
