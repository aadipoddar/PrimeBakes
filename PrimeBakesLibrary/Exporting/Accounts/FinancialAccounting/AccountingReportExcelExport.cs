using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingReportExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<AccountingOverviewModel> accountingData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        CompanyModel company = null,
        VoucherModel voucher = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(AccountingOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.VoucherId)] = new() { DisplayName = "Voucher ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(AccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            [nameof(AccountingOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(AccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(AccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            [nameof(AccountingOverviewModel.TotalDebitLedgers)] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditLedgers)] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalDebitAmount)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditAmount)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalAmount)] = new() { DisplayName = "Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
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
            columnOrder =
            [
                nameof(AccountingOverviewModel.TransactionNo),
                nameof(AccountingOverviewModel.TransactionDateTime),
                nameof(AccountingOverviewModel.ReferenceNo),
                nameof(AccountingOverviewModel.TotalDebitAmount),
                nameof(AccountingOverviewModel.TotalCreditAmount),
                nameof(AccountingOverviewModel.TotalAmount)
            ];

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

        string fileName = $"ACCOUNTING_REPORT";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
