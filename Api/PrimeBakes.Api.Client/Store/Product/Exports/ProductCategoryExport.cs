using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Exports;

public static class ProductCategoryExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductCategoryExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<ProductCategoryModel> productCategoryData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), productCategoryData, new { exportType });
}
