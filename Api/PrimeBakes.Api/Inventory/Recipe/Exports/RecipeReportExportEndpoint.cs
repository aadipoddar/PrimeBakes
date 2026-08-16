using PrimeBakes.Library.Inventory.Recipe.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Api.Inventory.Recipe.Exports;

public class RecipeReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RecipeReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RecipeReportExport.ExportReport), async (RecipeReportRequest request) =>
		{
			var (stream, fileName) = await RecipeReportExport.ExportReport(
				request.Data, request.ExportType, request.EffectiveDate, request.CostAsOnDate, request.Deduct);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(RecipeReportExport.ExportItemReport), async (RecipeItemReportRequest request) =>
		{
			var (stream, fileName) = await RecipeReportExport.ExportItemReport(
				request.Data, request.ExportType, request.EffectiveDate, request.CostAsOnDate, request.Deduct,
				request.RawMaterial, request.Category, request.Product);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
