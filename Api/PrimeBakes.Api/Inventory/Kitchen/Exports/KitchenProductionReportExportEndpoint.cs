using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Exports;

public class KitchenProductionReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenProductionReportExport.ExportReport), async (KitchenProductionReportRequest request) =>
		{
			var (stream, fileName) = await KitchenProductionReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(KitchenProductionReportExport.ExportItemReport), async (KitchenProductionItemReportRequest request) =>
		{
			var (stream, fileName) = await KitchenProductionReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.Product, request.ProductCategory, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
