using PrimeBakes.Library.Inventory.RawMaterial.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Api.Inventory.RawMaterial.Data;

public class RawMaterialDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialData.InsertRawMaterial), (RawMaterialModel rawMaterial) => RawMaterialData.InsertRawMaterial(rawMaterial));

		group.MapPost(nameof(RawMaterialData.DeleteTransaction), RawMaterialData.DeleteTransaction);
		group.MapPost(nameof(RawMaterialData.RecoverTransaction), RawMaterialData.RecoverTransaction);
		group.MapPost(nameof(RawMaterialData.SaveTransaction), RawMaterialData.SaveTransaction);
	}
}
