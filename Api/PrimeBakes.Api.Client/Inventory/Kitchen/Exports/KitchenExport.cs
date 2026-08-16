using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<KitchenModel> kitchenData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), kitchenData, new { exportType });
}
