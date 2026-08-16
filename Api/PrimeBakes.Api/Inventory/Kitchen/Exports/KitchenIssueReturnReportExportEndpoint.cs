using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Exports;

public class KitchenIssueReturnReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenIssueReturnReportExport.ExportReport), async (KitchenIssueReturnReportRequest request) =>
		{
			var (stream, fileName) = await KitchenIssueReturnReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(KitchenIssueReturnReportExport.ExportItemReport), async (KitchenIssueReturnItemReportRequest request) =>
		{
			var (stream, fileName) = await KitchenIssueReturnReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.RawMaterial, request.RawMaterialCategory, request.Kitchen, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
