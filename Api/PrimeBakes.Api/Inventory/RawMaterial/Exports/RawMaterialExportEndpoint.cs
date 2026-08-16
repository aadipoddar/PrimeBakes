using PrimeBakes.Library.Inventory.RawMaterial.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Api.Inventory.RawMaterial.Exports;

public class RawMaterialExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialExport.ExportMaster), async (List<RawMaterialModel> rawMaterialData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await RawMaterialExport.ExportMaster(rawMaterialData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
