using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.Masters.Exports;

public static class FinancialYearExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(FinancialYearExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<FinancialYearModel> financialYearData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), financialYearData, new { exportType });
}
