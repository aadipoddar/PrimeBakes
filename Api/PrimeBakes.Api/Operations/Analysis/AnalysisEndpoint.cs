using PrimeBakes.Api.Common;
using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Analysis;

public class AnalysisEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AnalysisEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(AnalysisData.LoadDashboardMonthlyTrend),
			(DateTime StartDate, DateTime EndDate) => AnalysisData.LoadDashboardMonthlyTrend(StartDate, EndDate));

		group.MapGet(nameof(AnalysisData.LoadDashboardTopProducts),
			(DateTime StartDate, DateTime EndDate) => AnalysisData.LoadDashboardTopProducts(StartDate, EndDate));

		group.MapGet(nameof(AnalysisData.LoadDashboardTopRawMaterials),
			(DateTime StartDate, DateTime EndDate) => AnalysisData.LoadDashboardTopRawMaterials(StartDate, EndDate));
	}
}
