using PrimeBakes.Library.Common;

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

public class AnalysisMonthlyTrendModel
{
	public int Year { get; set; }
	public int Month { get; set; }
	public decimal Revenue { get; set; }
	public decimal Purchase { get; set; }
}

public class AnalysisTopProductModel
{
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Amount { get; set; }
}

public class AnalysisTopRawMaterialModel
{
	public string ItemName { get; set; }
	public string UnitOfMeasurement { get; set; }
	public decimal Quantity { get; set; }
	public decimal Amount { get; set; }
}
