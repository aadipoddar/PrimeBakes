using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Library.Operations.User;

public static class UserExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(UserExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<UserModel> userData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), userData, new { exportType });
}
