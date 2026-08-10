using PrimeBakes.Library.Common;

namespace PrimeBakes.Library.Operations.Analysis;

public static class AnalysisData
{
	public static async Task<List<AnalysisMonthlyTrendModel>> LoadDashboardMonthlyTrend(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisMonthlyTrendModel, dynamic>(AnalysisNames.LoadDashboardMonthlyTrend, new { StartDate, EndDate });

	public static async Task<List<AnalysisOutletModel>> LoadDashboardRevenueByOutlet(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisOutletModel, dynamic>(AnalysisNames.LoadDashboardRevenueByOutlet, new { StartDate, EndDate });

	public static async Task<List<AnalysisPaymentModeModel>> LoadDashboardPaymentMix(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<AnalysisPaymentModeModel, dynamic>(AnalysisNames.LoadDashboardPaymentMix, new { StartDate, EndDate });
}

public class AnalysisMonthlyTrendModel
{
	public int Year { get; set; }
	public int Month { get; set; }
	public decimal Revenue { get; set; }
	public decimal Purchase { get; set; }
}

public class AnalysisOutletModel
{
	public string LocationCode { get; set; }
	public decimal Revenue { get; set; }
}

public class AnalysisPaymentModeModel
{
	public string PaymentMode { get; set; }
	public decimal Amount { get; set; }
}
