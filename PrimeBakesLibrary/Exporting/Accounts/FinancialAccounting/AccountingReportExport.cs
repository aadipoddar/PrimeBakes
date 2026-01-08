using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingReportExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<AccountingOverviewModel> accountingData,
        ReportExportType exportType,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        CompanyModel company = null,
        VoucherModel voucher = null)
    {
        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(AccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.TotalDebitLedgers)] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditLedgers)] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalDebitAmount)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditAmount)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalAmount)] = new() { DisplayName = "Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
        };

        List<string> columnOrder;

        if (showAllColumns)
        {
            columnOrder =
            [
                nameof(AccountingOverviewModel.TransactionNo),
                nameof(AccountingOverviewModel.TransactionDateTime),
                nameof(AccountingOverviewModel.CompanyName),
                nameof(AccountingOverviewModel.VoucherName),
                nameof(AccountingOverviewModel.ReferenceNo),
                nameof(AccountingOverviewModel.FinancialYear),
                nameof(AccountingOverviewModel.TotalDebitLedgers),
                nameof(AccountingOverviewModel.TotalCreditLedgers),
                nameof(AccountingOverviewModel.TotalDebitAmount),
                nameof(AccountingOverviewModel.TotalCreditAmount),
                nameof(AccountingOverviewModel.TotalAmount),
                nameof(AccountingOverviewModel.Remarks),
                nameof(AccountingOverviewModel.CreatedByName),
                nameof(AccountingOverviewModel.CreatedAt),
                nameof(AccountingOverviewModel.CreatedFromPlatform),
                nameof(AccountingOverviewModel.LastModifiedByUserName),
                nameof(AccountingOverviewModel.LastModifiedAt),
                nameof(AccountingOverviewModel.LastModifiedFromPlatform)
            ];

            if (company is not null)
                columnOrder.Remove(nameof(AccountingOverviewModel.CompanyName));

            if (voucher is not null)
                columnOrder.Remove(nameof(AccountingOverviewModel.VoucherName));
        }
        else
        {
            columnOrder =
            [
                nameof(AccountingOverviewModel.TransactionNo),
                nameof(AccountingOverviewModel.TransactionDateTime),
                nameof(AccountingOverviewModel.ReferenceNo),
                nameof(AccountingOverviewModel.TotalDebitAmount),
                nameof(AccountingOverviewModel.TotalCreditAmount),
                nameof(AccountingOverviewModel.TotalAmount)
            ];
        }

        string fileName = $"ACCOUNTING_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

        if (exportType == ReportExportType.PDF)
        {
            var stream = await PDFReportExportUtil.ExportToPdf(
                accountingData,
                "FINANCIAL ACCOUNTING REPORT",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                useBuiltInStyle: false,
                useLandscape: showAllColumns,
                new() { ["Company"] = company?.Name ?? null, ["Voucher"] = voucher?.Name ?? null }
            );

            fileName += ".pdf";
            return (stream, fileName);
        }
        else
        {
            var stream = await ExcelReportExportUtil.ExportToExcel(
                accountingData,
                "FINANCIAL ACCOUNTING REPORT",
                "Accounting Transactions",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                new() { ["Company"] = company?.Name ?? null, ["Voucher"] = voucher?.Name ?? null }
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }

    public static async Task<(MemoryStream stream, string fileName)> ExportLedgerReport(
        IEnumerable<AccountingLedgerOverviewModel> ledgerData,
        ReportExportType exportType,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        CompanyModel company = null,
        LedgerModel ledger = null,
        TrialBalanceModel trialBalance = null)
    {
        var columnSettings = new Dictionary<string, ReportColumnSetting>
        {
            [nameof(AccountingLedgerOverviewModel.LedgerName)] = new() { DisplayName = "Ledger", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.LedgerCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountTypeName)] = new() { DisplayName = "Account Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.GroupName)] = new() { DisplayName = "Group", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceType)] = new() { DisplayName = "Ref Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountingRemarks)] = new() { DisplayName = "Accounting Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.Remarks)] = new() { DisplayName = "Ledger Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceDateTime)] = new() { DisplayName = "Ref Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.Debit)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingLedgerOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
            [nameof(AccountingLedgerOverviewModel.ReferenceAmount)] = new() { DisplayName = "Ref Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
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

        string fileName = $"LEDGER_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

        if (exportType == ReportExportType.PDF)
        {
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

            fileName += ".pdf";
            return (stream, fileName);
        }
        else
        {
            var stream = await ExcelReportExportUtil.ExportToExcel(
                ledgerData,
                "FINANCIAL LEDGER REPORT",
                "Ledger Report",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                new() { ["Company"] = company?.Name ?? null, ["Ledger"] = ledger?.Name ?? null },
                customSummaryFields
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }
}
