using PrimeBakes.Library.Inventory.Recipe.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Inventory.Recipe.Exports;

public class RecipeInvoiceExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RecipeInvoiceExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RecipeInvoiceExport.ExportInvoice), async (int transactionId, InvoiceExportType exportType, DateTime? costAsOnDateTime) =>
		{
			var (stream, fileName) = await RecipeInvoiceExport.ExportInvoice(transactionId, exportType, costAsOnDateTime);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
