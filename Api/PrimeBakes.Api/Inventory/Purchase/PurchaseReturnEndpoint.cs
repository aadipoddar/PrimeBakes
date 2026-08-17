using PrimeBakes.Data.Inventory.Purchase;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Api.Inventory.Purchase;

public class PurchaseReturnEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseReturnEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(PurchaseReturnData.LoadInvoiceBundle),
			(int transactionId) => PurchaseReturnData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(PurchaseReturnData.DeleteTransaction), (PurchaseReturnModel purchaseReturn) => PurchaseReturnData.DeleteTransaction(purchaseReturn));
		group.MapPost(nameof(PurchaseReturnData.RecoverTransaction), (PurchaseReturnModel purchaseReturn) => PurchaseReturnData.RecoverTransaction(purchaseReturn));
		group.MapPost(nameof(PurchaseReturnData.SaveTransaction), (PurchaseReturnSaveRequest request) => PurchaseReturnData.SaveTransaction(request.PurchaseReturn, request.Details, request.Recover));
	}
}
