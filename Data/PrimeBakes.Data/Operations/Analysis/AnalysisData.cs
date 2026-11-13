using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.Analysis;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Data.Operations.Analysis;

public static class AnalysisData
{
	public static async Task<List<AnalysisMonthlyTrendModel>> LoadDashboardMonthlyTrend(DateTime StartDate, DateTime EndDate)
	{
		if (!OfflineState.LocalDBAvailable)
			await QueryGate.EnsureCapacity(StartDate, EndDate);

		return await SqlDataAccess.LoadData<AnalysisMonthlyTrendModel, dynamic>(AnalysisNames.LoadDashboardMonthlyTrend, new { StartDate, EndDate }, null, true);
	}

	public static async Task<List<AnalysisTopProductModel>> LoadDashboardTopProducts(DateTime StartDate, DateTime EndDate)
	{
		if (!OfflineState.LocalDBAvailable)
			await QueryGate.EnsureCapacity(StartDate, EndDate);

		return await SqlDataAccess.LoadData<AnalysisTopProductModel, dynamic>(AnalysisNames.LoadDashboardTopProducts, new { StartDate, EndDate }, null, true);
	}

	public static async Task<List<AnalysisTopRawMaterialModel>> LoadDashboardTopRawMaterials(DateTime StartDate, DateTime EndDate)
	{
		if (!OfflineState.LocalDBAvailable)
			await QueryGate.EnsureCapacity(StartDate, EndDate);

		return await SqlDataAccess.LoadData<AnalysisTopRawMaterialModel, dynamic>(AnalysisNames.LoadDashboardTopRawMaterials, new { StartDate, EndDate }, null, true);

	}

	public static async Task<List<string>> LoadPageRecommendations(int userId)
	{
		var pageVisits = await AuditTrailData.LoadAuditTrailByActionUserIdDate(
			AuditTrailActionTypes.PageVisit, userId, DateTime.Now.AddDays(-7), DateTime.Now);

		return [.. pageVisits
			.Where(visit => !string.IsNullOrWhiteSpace(visit.TableName))
			.GroupBy(visit => visit.TableName, StringComparer.OrdinalIgnoreCase)
			.OrderByDescending(group => group.Count())
			.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
			.Take(2)
			.Select(group => group.Key)];
	}
}
