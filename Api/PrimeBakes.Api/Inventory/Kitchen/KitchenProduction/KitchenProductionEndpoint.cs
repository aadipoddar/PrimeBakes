using PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

namespace PrimeBakes.Api.Inventory.Kitchen.KitchenProduction;

public class KitchenProductionEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenProductionEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(KitchenProductionData.LoadInvoiceBundle),
			(int transactionId) => KitchenProductionData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(KitchenProductionData.DeleteTransaction), (KitchenProductionModel kitchenProduction) => KitchenProductionData.DeleteTransaction(kitchenProduction));
		group.MapPost(nameof(KitchenProductionData.RecoverTransaction), (KitchenProductionModel kitchenProduction) => KitchenProductionData.RecoverTransaction(kitchenProduction));
		group.MapPost(nameof(KitchenProductionData.SaveTransaction), (KitchenProductionSaveRequest request) => KitchenProductionData.SaveTransaction(request.KitchenProduction, request.Details, request.Recover));
	}
}
