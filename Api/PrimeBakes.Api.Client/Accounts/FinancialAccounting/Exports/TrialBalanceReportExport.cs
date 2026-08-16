using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class TrialBalanceReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(TrialBalanceReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<TrialBalanceModel> trialBalanceData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null,
		GroupModel group = null,
		AccountTypeModel accountType = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new TrialBalanceReportRequest(trialBalanceData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company, group, accountType));
}
