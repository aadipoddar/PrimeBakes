using PrimeBakes.Library.Inventory.Stock.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Api.Inventory.Stock.Exports;

public class ProductStockReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductStockReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductStockReportExport.ExportSummaryReport), async (ProductStockSummaryReportRequest request) =>
		{
			var (stream, fileName) = await ProductStockReportExport.ExportSummaryReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd, request.ShowAllColumns, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(ProductStockReportExport.ExportDetailsReport), async (ProductStockDetailsReportRequest request) =>
		{
			var (stream, fileName) = await ProductStockReportExport.ExportDetailsReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd, request.Location);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
