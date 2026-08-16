using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.Masters.Exports;

public static class LedgerExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(LedgerExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<LedgerModel> ledgerData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), ledgerData, new { exportType });
}
