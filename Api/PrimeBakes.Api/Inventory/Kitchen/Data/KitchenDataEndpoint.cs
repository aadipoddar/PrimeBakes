using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Inventory.Kitchen.Data;

public class KitchenDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenData.DeleteTransaction), KitchenData.DeleteTransaction);
		group.MapPost(nameof(KitchenData.RecoverTransaction), KitchenData.RecoverTransaction);
		group.MapPost(nameof(KitchenData.SaveTransaction), KitchenData.SaveTransaction);
	}
}
