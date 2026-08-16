using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.RawMaterial.Exports;

public static class RawMaterialCategoryExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialCategoryExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<RawMaterialCategoryModel> categoryData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), categoryData, new { exportType });
}
