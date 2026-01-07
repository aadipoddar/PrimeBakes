using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingLedgerReportPdfExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<AccountingLedgerOverviewModel> ledgerData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        CompanyModel company = null,
        LedgerModel ledger = null,
        TrialBalanceModel trialBalance = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(AccountingLedgerOverviewModel.LedgerName)] = new() { DisplayName = "Ledger", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.LedgerCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountTypeName)] = new() { DisplayName = "Account Type", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.GroupName)] = new() { DisplayName = "Group", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceType)] = new() { DisplayName = "Ref Type", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountingRemarks)] = new() { DisplayName = "Accounting Remarks", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.Remarks)] = new() { DisplayName = "Ledger Remarks", IncludeInTotal = false },

            [nameof(AccountingLedgerOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceDateTime)] = new() { DisplayName = "Ref Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },

            [nameof(AccountingLedgerOverviewModel.Debit)] = new()
            {
                DisplayName = "Debit",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = true
            },

            [nameof(AccountingLedgerOverviewModel.Credit)] = new()
            {
                DisplayName = "Credit",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = true
            },

            [nameof(AccountingLedgerOverviewModel.ReferenceAmount)] = new()
            {
                DisplayName = "Ref Amount",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = true
            }
        };

        List<string> columnOrder;

        if (showAllColumns)
        {
            columnOrder =
            [
                nameof(AccountingLedgerOverviewModel.LedgerName),
                nameof(AccountingLedgerOverviewModel.AccountTypeName),
                nameof(AccountingLedgerOverviewModel.GroupName),
                nameof(AccountingLedgerOverviewModel.CompanyName),
                nameof(AccountingLedgerOverviewModel.TransactionNo),
                nameof(AccountingLedgerOverviewModel.TransactionDateTime),
                nameof(AccountingLedgerOverviewModel.ReferenceType),
                nameof(AccountingLedgerOverviewModel.ReferenceNo),
                nameof(AccountingLedgerOverviewModel.ReferenceDateTime),
                nameof(AccountingLedgerOverviewModel.ReferenceAmount),
                nameof(AccountingLedgerOverviewModel.Debit),
                nameof(AccountingLedgerOverviewModel.Credit),
                nameof(AccountingLedgerOverviewModel.AccountingRemarks),
                nameof(AccountingLedgerOverviewModel.Remarks)
            ];

            if (ledger is not null)
                columnOrder.Remove(nameof(AccountingLedgerOverviewModel.LedgerName));

            if (company is not null)
                columnOrder.Remove(nameof(AccountingLedgerOverviewModel.CompanyName));
        }

        else
        {
            columnOrder =
            [
                nameof(AccountingLedgerOverviewModel.LedgerName),
                nameof(AccountingLedgerOverviewModel.TransactionNo),
                nameof(AccountingLedgerOverviewModel.TransactionDateTime),
                nameof(AccountingLedgerOverviewModel.ReferenceNo),
                nameof(AccountingLedgerOverviewModel.Debit),
                nameof(AccountingLedgerOverviewModel.Credit)
            ];

            if (ledger is not null)
                columnOrder.Remove(nameof(AccountingLedgerOverviewModel.LedgerName));
        }

        Dictionary<string, string> customSummaryFields = null;
        if (trialBalance is not null)
            customSummaryFields = new Dictionary<string, string>
            {
                ["Opening Balance"] = $"₹ {trialBalance.OpeningBalance:N2}",
                ["Closing Balance"] = $"₹ {trialBalance.ClosingBalance:N2}"
            };

        var stream = await PDFReportExportUtil.ExportToPdf(
            ledgerData,
            "FINANCIAL LEDGER REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            useLandscape: showAllColumns,
            new() { ["Company"] = company?.Name ?? null, ["Ledger"] = ledger?.Name ?? null },
            customSummaryFields: customSummaryFields
        );

        string fileName = $"LEDGER_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".pdf";

        return (stream, fileName);
    }
}
