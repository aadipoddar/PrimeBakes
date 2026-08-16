using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Exports;

public class KitchenIssueReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenIssueReportExport.ExportReport), async (KitchenIssueReportRequest request) =>
		{
			var (stream, fileName) = await KitchenIssueReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(KitchenIssueReportExport.ExportItemReport), async (KitchenIssueItemReportRequest request) =>
		{
			var (stream, fileName) = await KitchenIssueReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.RawMaterial, request.RawMaterialCategory, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
