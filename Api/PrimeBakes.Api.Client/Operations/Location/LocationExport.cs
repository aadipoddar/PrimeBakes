using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Library.Operations.Location;

public static class LocationExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(LocationExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<LocationModel> locationData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), locationData, new { exportType });
}
