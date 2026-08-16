using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.FinancialAccounting.Exports;

public static class BalanceSheetReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BalanceSheetReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportAssetsReport(
		IEnumerable<TrialBalanceModel> assetsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportAssetsReport)),
			new BalanceSheetReportRequest(assetsData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportLiabilitiesReport(
		IEnumerable<TrialBalanceModel> liabilitiesData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportLiabilitiesReport)),
			new BalanceSheetReportRequest(liabilitiesData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));
}
