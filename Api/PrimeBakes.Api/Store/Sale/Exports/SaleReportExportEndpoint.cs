using PrimeBakes.Library.Store.Sale.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale.Exports;

public class SaleReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(SaleReportExport.ExportReport), async (SaleReportRequest request) =>
		{
			var (stream, fileName) = await SaleReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Party, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(SaleReportExport.ExportItemReport), async (SaleItemReportRequest request) =>
		{
			var (stream, fileName) = await SaleReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Company, request.Location, request.Party);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
