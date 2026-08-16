using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Library.Accounts.Masters.Exports;

public static class GroupExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(GroupExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<GroupModel> groupData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), groupData, new { exportType });
}
