using PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

namespace PrimeBakes.Api.Inventory.Kitchen.KitchenProduction;

public class KitchenProductionReturnEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReturnEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(KitchenProductionReturnData.LoadInvoiceBundle),
			(int transactionId) => KitchenProductionReturnData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(KitchenProductionReturnData.DeleteTransaction), (KitchenProductionReturnModel kitchenProductionReturn) => KitchenProductionReturnData.DeleteTransaction(kitchenProductionReturn));
		group.MapPost(nameof(KitchenProductionReturnData.RecoverTransaction), (KitchenProductionReturnModel kitchenProductionReturn) => KitchenProductionReturnData.RecoverTransaction(kitchenProductionReturn));
		group.MapPost(nameof(KitchenProductionReturnData.SaveTransaction), (KitchenProductionReturnSaveRequest request) => KitchenProductionReturnData.SaveTransaction(request.KitchenProductionReturn, request.Details, request.Recover));
	}
}
