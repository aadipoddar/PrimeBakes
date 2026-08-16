using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class ProfitAndLossReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProfitAndLossReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportIncomeReport(
		IEnumerable<TrialBalanceModel> incomeData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportIncomeReport)),
			new ProfitAndLossReportRequest(incomeData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportExpenseReport(
		IEnumerable<TrialBalanceModel> expenseData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportExpenseReport)),
			new ProfitAndLossReportRequest(expenseData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));
}
