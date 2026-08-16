using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Analysis;

namespace PrimeBakes.Library.Operations.Analysis;

public static class AnalysisData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AnalysisData));

	public static async Task<List<AnalysisMonthlyTrendModel>> LoadDashboardMonthlyTrend(DateTime StartDate, DateTime EndDate) =>
		await ApiClient.Get<List<AnalysisMonthlyTrendModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadDashboardMonthlyTrend)), new { StartDate, EndDate });

	public static async Task<List<AnalysisTopProductModel>> LoadDashboardTopProducts(DateTime StartDate, DateTime EndDate) =>
		await ApiClient.Get<List<AnalysisTopProductModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadDashboardTopProducts)), new { StartDate, EndDate });

	public static async Task<List<AnalysisTopRawMaterialModel>> LoadDashboardTopRawMaterials(DateTime StartDate, DateTime EndDate) =>
		await ApiClient.Get<List<AnalysisTopRawMaterialModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadDashboardTopRawMaterials)), new { StartDate, EndDate });
}
