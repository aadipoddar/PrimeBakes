using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Library.Restaurant.Dining.Exports;

public static class DiningTableExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DiningTableExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<DiningTableModel> diningTableData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), diningTableData, new { exportType });
}
