using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Exports;

public static class ProductExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<ProductModel> productData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), productData, new { exportType });
}
