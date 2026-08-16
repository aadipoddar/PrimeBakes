using PrimeBakes.Library.Store.Customer.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Api.Store.Customer.Exports;

public class CustomerSummaryReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CustomerSummaryReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CustomerSummaryReportExport.ExportReport), async (CustomerSummaryReportRequest request) =>
		{
			var (stream, fileName) = await CustomerSummaryReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
