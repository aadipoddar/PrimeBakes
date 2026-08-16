using PrimeBakes.Library.Store.StockTransfer.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Api.Store.StockTransfer.Exports;

public class StockTransferReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(StockTransferReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(StockTransferReportExport.ExportReport), async (StockTransferReportRequest request) =>
		{
			var (stream, fileName) = await StockTransferReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Company, request.FromLocation, request.ToLocation);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(StockTransferReportExport.ExportItemReport), async (StockTransferItemReportRequest request) =>
		{
			var (stream, fileName) = await StockTransferReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Company, request.FromLocation, request.ToLocation);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
