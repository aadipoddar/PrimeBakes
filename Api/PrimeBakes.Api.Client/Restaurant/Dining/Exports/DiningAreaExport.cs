using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Library.Restaurant.Dining.Exports;

public static class DiningAreaExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DiningAreaExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<DiningAreaModel> diningAreaData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), diningAreaData, new { exportType });
}
