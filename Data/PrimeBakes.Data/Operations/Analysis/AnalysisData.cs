using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.Analysis;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;

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
		var showRecommendation = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.ShowPageRecommendations)).Value);
		if (!showRecommendation)
			return [];

		var historyDays = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.RecommendationHistoryDays)).Value);

		var allVisits = (await AuditTrailData.LoadAuditTrailByActionUserIdDate(
				AuditTrailActionTypes.PageVisit, userId, DateTime.Now.AddDays(-historyDays), DateTime.Now))
			.Where(visit => !string.IsNullOrWhiteSpace(visit.TableName))
			.Where(visit => !_excludedPages.Contains(visit.TableName))
			.ToList();

		// Visits that happened within 30 minutes of the current time of day (on any day)
		var nowMinutes = DateTime.Now.TimeOfDay.TotalMinutes;
		var visitsAroundNow = allVisits.Where(visit => IsWithin30Minutes(visit.TransactionDateTime, nowMinutes));

		// 1st pick: most visited page around this time (empty if nothing found)
		var timeBasedPick = TopPages(visitsAroundNow, count: 1, exclude: []);

		// Remaining slots: most visited pages overall (2 if no time-based pick, otherwise 1)
		var frequentPicks = TopPages(allVisits, count: 2 - timeBasedPick.Count, exclude: timeBasedPick);

		return [.. timeBasedPick, .. frequentPicks];
	}

	private static bool IsWithin30Minutes(DateTime visitTime, double nowMinutes)
	{
		var difference = Math.Abs(visitTime.TimeOfDay.TotalMinutes - nowMinutes);
		return Math.Min(difference, 1440 - difference) <= 30; // 1440 = minutes in a day, handles midnight wrap
	}

	private static List<string> TopPages(IEnumerable<AuditTrailModel> visits, int count, IEnumerable<string> exclude) =>
		[.. visits
		.GroupBy(visit => visit.TableName, StringComparer.OrdinalIgnoreCase)
		.Where(group => !exclude.Contains(group.Key, StringComparer.OrdinalIgnoreCase))
		.OrderByDescending(group => group.Count())
		.ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
		.Take(count)
		.Select(group => group.Key)];

	// Pages that make no sense as a recommendation (mid-flow steps, home, login)
	private static readonly HashSet<string> _excludedPages =
	[
		with(StringComparer.OrdinalIgnoreCase),
		StoreRouteNames.OrderMobileCart,
		StoreRouteNames.OrderMobileConfirmation,

		StoreRouteNames.SaleMobileCart,
		StoreRouteNames.SaleMobilePayment,
		StoreRouteNames.SaleMobileConfirmation,

		RestaurantRouteNames.BillMobileCart,
		RestaurantRouteNames.BillMobilePayment,
		RestaurantRouteNames.BillMobileConfirmation
	];

}