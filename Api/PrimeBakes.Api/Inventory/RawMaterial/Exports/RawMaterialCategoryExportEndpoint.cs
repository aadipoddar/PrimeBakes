using PrimeBakes.Library.Inventory.RawMaterial.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Api.Inventory.RawMaterial.Exports;

public class RawMaterialCategoryExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialCategoryExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialCategoryExport.ExportMaster), async (List<RawMaterialCategoryModel> categoryData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await RawMaterialCategoryExport.ExportMaster(categoryData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
