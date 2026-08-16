using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Analysis;

namespace PrimeBakes.Library.Operations.Analysis;

public static class AnalysisData
{
	public static async Task<List<AnalysisMonthlyTrendModel>> LoadDashboardMonthlyTrend(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisMonthlyTrendModel, dynamic>(AnalysisNames.LoadDashboardMonthlyTrend, new { StartDate, EndDate });

	public static async Task<List<AnalysisTopProductModel>> LoadDashboardTopProducts(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisTopProductModel, dynamic>(AnalysisNames.LoadDashboardTopProducts, new { StartDate, EndDate });

	public static async Task<List<AnalysisTopRawMaterialModel>> LoadDashboardTopRawMaterials(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisTopRawMaterialModel, dynamic>(AnalysisNames.LoadDashboardTopRawMaterials, new { StartDate, EndDate });
}
