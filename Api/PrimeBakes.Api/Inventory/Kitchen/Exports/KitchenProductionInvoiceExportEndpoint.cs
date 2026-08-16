using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Inventory.Kitchen.Exports;

public class KitchenProductionInvoiceExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionInvoiceExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenProductionInvoiceExport.ExportInvoice), async (int transactionId, InvoiceExportType exportType) =>
		{
			var (stream, fileName) = await KitchenProductionInvoiceExport.ExportInvoice(transactionId, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
