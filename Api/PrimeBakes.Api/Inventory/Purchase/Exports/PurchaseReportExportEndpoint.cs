using PrimeBakes.Library.Inventory.Purchase.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Api.Inventory.Purchase.Exports;

public class PurchaseReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(PurchaseReportExport.ExportReport), async (PurchaseReportRequest request) =>
		{
			var (stream, fileName) = await PurchaseReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary, request.Party, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(PurchaseReportExport.ExportItemReport), async (PurchaseItemReportRequest request) =>
		{
			var (stream, fileName) = await PurchaseReportExport.ExportItemReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.ShowSummary,
				request.RawMaterial, request.RawMaterialCategory, request.Company, request.Party);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
