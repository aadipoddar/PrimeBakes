using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Data;

public class KitchenProductionReturnDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReturnDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenProductionReturnData.DeleteTransaction), (KitchenProductionReturnModel kitchenProductionReturn) => KitchenProductionReturnData.DeleteTransaction(kitchenProductionReturn));
		group.MapPost(nameof(KitchenProductionReturnData.RecoverTransaction), (KitchenProductionReturnModel kitchenProductionReturn) => KitchenProductionReturnData.RecoverTransaction(kitchenProductionReturn));
		group.MapPost(nameof(KitchenProductionReturnData.SaveTransaction), (KitchenProductionReturnSaveRequest request) => KitchenProductionReturnData.SaveTransaction(request.KitchenProductionReturn, request.Details, request.Recover));
	}
}
