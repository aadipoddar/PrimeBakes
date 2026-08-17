using PrimeBakes.Data.Inventory.RawMaterial;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Api.Inventory.RawMaterial;

public class RawMaterialEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialData.InsertRawMaterial), (RawMaterialModel rawMaterial) => RawMaterialData.InsertRawMaterial(rawMaterial));

		group.MapPost(nameof(RawMaterialData.DeleteTransaction), RawMaterialData.DeleteTransaction);
		group.MapPost(nameof(RawMaterialData.RecoverTransaction), RawMaterialData.RecoverTransaction);
		group.MapPost(nameof(RawMaterialData.SaveTransaction), RawMaterialData.SaveTransaction);
	}
}
