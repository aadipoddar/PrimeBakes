using PrimeBakes.Data.Inventory.RawMaterial;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Inventory.RawMaterial;

public class RawMaterialCategoryEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialCategoryEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialCategoryData.DeleteTransaction), RawMaterialCategoryData.DeleteTransaction);
		group.MapPost(nameof(RawMaterialCategoryData.RecoverTransaction), RawMaterialCategoryData.RecoverTransaction);
		group.MapPost(nameof(RawMaterialCategoryData.SaveTransaction), RawMaterialCategoryData.SaveTransaction);
	}
}
