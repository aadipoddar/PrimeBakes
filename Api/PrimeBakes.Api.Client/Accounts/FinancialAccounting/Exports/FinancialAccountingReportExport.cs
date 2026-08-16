using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class FinancialAccountingReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(FinancialAccountingReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<FinancialAccountingOverviewModel> accountingData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		CompanyModel company = null,
		VoucherModel voucher = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new FinancialAccountingReportRequest(accountingData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, company, voucher));

	public static async Task<(MemoryStream stream, string fileName)> ExportLedgerReport(
		IEnumerable<FinancialAccountingLedgerOverviewModel> ledgerData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = true,
		CompanyModel company = null,
		LedgerModel ledger = null,
		TrialBalanceModel trialBalance = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportLedgerReport)),
			new FinancialAccountingLedgerReportRequest(ledgerData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, company, ledger, trialBalance));
}
