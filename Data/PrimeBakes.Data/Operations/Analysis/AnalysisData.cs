using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.Analysis;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Data.Operations.Analysis;

public static class AnalysisData
{
	#region Trend Analysis
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
	#endregion

	#region Page Recommendation
	public static async Task<List<string>> LoadPageRecommendations(int userId)
	{
		var showRecommendation = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.ShowPageRecommendations)).Value);
		var historyDays = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.RecommendationHistoryDays)).Value);
		if (!showRecommendation)
			return [];

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
	#endregion

	#region Product Recommendation
	public static async Task<List<ProductModel>> LoadProductRecommendations(int locationId, bool isOrder = false, bool isSale = false, bool isBill = false)
	{
		var showRecommendation = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.ShowPageRecommendations)).Value);
		var historyDays = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.RecommendationHistoryDays)).Value);
		if (!showRecommendation)
			return [];

		var fromDate = DateTime.Today.AddDays(-historyDays);
		var toDate = DateTime.Today;

		var items =
			 isOrder ? (await CommonData.LoadTableDataByDate<OrderItemOverviewModel>(StoreNames.OrderItemOverview, fromDate, toDate, useLocalDB: true))
				.Select(item => (item.LocationId, item.MasterStatus, item.MasterId, item.ItemId, item.Quantity))
			: isSale ? (await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate, useLocalDB: true))
				.Select(item => (item.LocationId, item.MasterStatus, item.MasterId, item.ItemId, item.Quantity))
			: isBill ? (await CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate, useLocalDB: true))
				.Select(item => (item.LocationId, item.MasterStatus, item.MasterId, item.ItemId, item.Quantity))
			: [];

		var products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product, useLocalDB: true);
		var productsById = products.ToDictionary(product => product.Id);

		return [.. items
			.Where(item => item.LocationId == locationId && item.MasterStatus && productsById.ContainsKey(item.ItemId))
			.GroupBy(item => item.ItemId)
			.OrderByDescending(group => group.Select(item => item.MasterId).Distinct().Count()) // in how many orders / sales / bills
			.ThenByDescending(group => group.Sum(item => item.Quantity))
			.ThenBy(group => group.Key)
			.Take(20)
			.Select(group => productsById[group.Key])];
	}
	#endregion
}