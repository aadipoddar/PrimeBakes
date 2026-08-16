using PrimeBakes.Library.Restaurant.Bill.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Api.Restaurant.Bill.Exports;

public class BillReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BillReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(BillReportExport.ExportReport), async (BillReportRequest request) =>
		{
			var (stream, fileName) = await BillReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(BillReportExport.ExportItemReport), async (BillItemReportRequest request) =>
		{
			var (stream, fileName) = await BillReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
