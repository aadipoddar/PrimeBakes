using PrimeBakes.Library.Inventory.RawMaterial.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Inventory.RawMaterial.Data;

public class RawMaterialCategoryDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RawMaterialCategoryDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(RawMaterialCategoryData.DeleteTransaction), RawMaterialCategoryData.DeleteTransaction);
		group.MapPost(nameof(RawMaterialCategoryData.RecoverTransaction), RawMaterialCategoryData.RecoverTransaction);
		group.MapPost(nameof(RawMaterialCategoryData.SaveTransaction), RawMaterialCategoryData.SaveTransaction);
	}
}
