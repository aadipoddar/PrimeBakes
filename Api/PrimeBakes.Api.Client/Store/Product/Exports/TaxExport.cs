using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Exports;

public static class TaxExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(TaxExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<TaxModel> taxData, ReportExportType exportType) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportMaster)), taxData, new { exportType });
}
