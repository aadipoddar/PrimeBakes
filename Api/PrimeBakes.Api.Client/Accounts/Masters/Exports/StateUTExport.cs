using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.Masters.Exports;

public static class StateUTExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(StateUTExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<StateUTModel> stateUTData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), stateUTData, new { exportType });
}
