using PrimeBakes.Library.Inventory.Purchase.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Api.Inventory.Purchase.Data;

public class PurchaseReturnDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseReturnDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(PurchaseReturnData.DeleteTransaction), (PurchaseReturnModel purchaseReturn) => PurchaseReturnData.DeleteTransaction(purchaseReturn));
		group.MapPost(nameof(PurchaseReturnData.RecoverTransaction), (PurchaseReturnModel purchaseReturn) => PurchaseReturnData.RecoverTransaction(purchaseReturn));
		group.MapPost(nameof(PurchaseReturnData.SaveTransaction), (PurchaseReturnSaveRequest request) => PurchaseReturnData.SaveTransaction(request.PurchaseReturn, request.Details, request.Recover));
	}
}
