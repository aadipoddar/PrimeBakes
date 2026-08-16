using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Data;

public class KitchenProductionDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenProductionData.DeleteTransaction), (KitchenProductionModel kitchenProduction) => KitchenProductionData.DeleteTransaction(kitchenProduction));
		group.MapPost(nameof(KitchenProductionData.RecoverTransaction), (KitchenProductionModel kitchenProduction) => KitchenProductionData.RecoverTransaction(kitchenProduction));
		group.MapPost(nameof(KitchenProductionData.SaveTransaction), (KitchenProductionSaveRequest request) => KitchenProductionData.SaveTransaction(request.KitchenProduction, request.Details, request.Recover));
	}
}
