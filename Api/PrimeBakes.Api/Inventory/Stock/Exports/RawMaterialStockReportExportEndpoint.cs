using PrimeBakes.Library.Inventory.Stock.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Api.Inventory.Stock.Exports;

public class RawMaterialStockReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialStockReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialStockReportExport.ExportSummaryReport), async (RawMaterialStockSummaryReportRequest request) =>
		{
			var (stream, fileName) = await RawMaterialStockReportExport.ExportSummaryReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd, request.ShowAllColumns);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(RawMaterialStockReportExport.ExportDetailsReport), async (RawMaterialStockDetailsReportRequest request) =>
		{
			var (stream, fileName) = await RawMaterialStockReportExport.ExportDetailsReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
