using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class TrialBalanceExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportReport(
        IEnumerable<TrialBalanceModel> trialBalanceData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        CompanyModel company = null,
        GroupModel group = null,
        AccountTypeModel accountType = null)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(TrialBalanceModel.LedgerId)] = new() { DisplayName = "Ledger ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TrialBalanceModel.GroupId)] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TrialBalanceModel.AccountTypeId)] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            [nameof(TrialBalanceModel.LedgerCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.LedgerName)] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.GroupName)] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.AccountTypeName)] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            [nameof(TrialBalanceModel.OpeningDebit)] = new() { DisplayName = "Opening Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.OpeningCredit)] = new() { DisplayName = "Opening Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.OpeningBalance)] = new() { DisplayName = "Opening Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.Debit)] = new() { DisplayName = "Period Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.Credit)] = new() { DisplayName = "Period Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingDebit)] = new() { DisplayName = "Closing Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingCredit)] = new() { DisplayName = "Closing Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingBalance)] = new() { DisplayName = "Closing Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        List<string> columnOrder;

        if (showAllColumns)
        {
            columnOrder =
            [
                nameof(TrialBalanceModel.LedgerName),
                nameof(TrialBalanceModel.LedgerCode),
                nameof(TrialBalanceModel.GroupName),
                nameof(TrialBalanceModel.AccountTypeName),
                nameof(TrialBalanceModel.OpeningBalance),
                nameof(TrialBalanceModel.OpeningDebit),
                nameof(TrialBalanceModel.OpeningCredit),
                nameof(TrialBalanceModel.Debit),
                nameof(TrialBalanceModel.Credit),
                nameof(TrialBalanceModel.ClosingBalance),
                nameof(TrialBalanceModel.ClosingDebit),
                nameof(TrialBalanceModel.ClosingCredit),
            ];

            if (group is not null)
                columnOrder.Remove(nameof(TrialBalanceModel.GroupName));

            if (accountType is not null)
                columnOrder.Remove(nameof(TrialBalanceModel.AccountTypeName));
        }

        else
            columnOrder =
            [
                nameof(TrialBalanceModel.LedgerName),
                nameof(TrialBalanceModel.OpeningBalance),
                nameof(TrialBalanceModel.Debit),
                nameof(TrialBalanceModel.Credit),
                nameof(TrialBalanceModel.ClosingBalance)
            ];

        var stream = await ExcelReportExportUtil.ExportToExcel(
            trialBalanceData,
            "TRIAL BALANCE REPORT",
            "Trial Balance",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            new() { ["Company Name"] = company?.Name ?? null, ["Group Name"] = group?.Name ?? null, ["Account Type"] = accountType?.Name ?? null }
        );

        string fileName = $"TRIAL_BALANCE";
        if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
            fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
        fileName += ".xlsx";

        return (stream, fileName);
    }
}
