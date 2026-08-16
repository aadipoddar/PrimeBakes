using PrimeBakes.Library.Store.Sale.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale.Exports;

public class SaleReturnReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleReturnReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(SaleReturnReportExport.ExportReport), async (SaleReturnReportRequest request) =>
		{
			var (stream, fileName) = await SaleReturnReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Party, request.Company, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(SaleReturnReportExport.ExportItemReport), async (SaleReturnItemReportRequest request) =>
		{
			var (stream, fileName) = await SaleReturnReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Company, request.Location, request.Party);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
