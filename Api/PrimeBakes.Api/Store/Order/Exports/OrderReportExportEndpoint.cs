using PrimeBakes.Library.Store.Order.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Order;

namespace PrimeBakes.Api.Store.Order.Exports;

public class OrderReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(OrderReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(OrderReportExport.ExportReport), async (OrderReportRequest request) =>
		{
			var (stream, fileName) = await OrderReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(OrderReportExport.ExportItemReport), async (OrderItemReportRequest request) =>
		{
			var (stream, fileName) = await OrderReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
