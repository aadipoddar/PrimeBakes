using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Exports;

public static class ProductLocationExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductLocationExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<ProductLocationOverviewModel> productLocationData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), productLocationData, new { exportType });
}
