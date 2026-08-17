using PrimeBakes.Data.Inventory.Kitchen;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Inventory.Kitchen;

public class KitchenEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenData.DeleteTransaction), KitchenData.DeleteTransaction);
		group.MapPost(nameof(KitchenData.RecoverTransaction), KitchenData.RecoverTransaction);
		group.MapPost(nameof(KitchenData.SaveTransaction), KitchenData.SaveTransaction);
	}
}
