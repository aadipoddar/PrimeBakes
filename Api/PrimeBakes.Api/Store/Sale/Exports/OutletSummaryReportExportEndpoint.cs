using PrimeBakes.Library.Store.Sale.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale.Exports;

public class OutletSummaryReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(OutletSummaryReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(OutletSummaryReportExport.ExportReport), async (OutletSummaryReportRequest request) =>
		{
			var (stream, fileName) = await OutletSummaryReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd, request.ShowAllColumns, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
