using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.RawMaterial.Exports;

public static class RawMaterialExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<RawMaterialModel> rawMaterialData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), rawMaterialData, new { exportType });
}
