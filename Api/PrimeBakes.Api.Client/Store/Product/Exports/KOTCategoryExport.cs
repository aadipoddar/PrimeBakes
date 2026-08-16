using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Exports;

public static class KOTCategoryExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KOTCategoryExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<KOTCategoryModel> kotCategoryData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), kotCategoryData, new { exportType });
}
