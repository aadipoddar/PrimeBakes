using PrimeBakes.Data.Inventory.PurchaseOrder;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;

namespace PrimeBakes.Api.Inventory.PurchaseOrder;

public class PurchaseOrderEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseOrderEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(PurchaseOrderData.LoadPurchaseOrderByPartyPending),
			(int PartyId) => PurchaseOrderData.LoadPurchaseOrderByPartyPending(PartyId));

		group.MapGet(nameof(PurchaseOrderData.LoadInvoiceBundle),
			(int transactionId) => PurchaseOrderData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(PurchaseOrderData.LinkPurchaseOrderToPurchase),
			(int? purchaseOrderId, int? purchaseId, bool unlink) => PurchaseOrderData.LinkPurchaseOrderToPurchase(purchaseOrderId, purchaseId, unlink));

		group.MapPost(nameof(PurchaseOrderData.DeleteTransaction), (PurchaseOrderModel purchaseOrder) => PurchaseOrderData.DeleteTransaction(purchaseOrder));
		group.MapPost(nameof(PurchaseOrderData.RecoverTransaction), (PurchaseOrderModel purchaseOrder) => PurchaseOrderData.RecoverTransaction(purchaseOrder));
		group.MapPost(nameof(PurchaseOrderData.SaveTransaction), (PurchaseOrderSaveRequest request) => PurchaseOrderData.SaveTransaction(request.PurchaseOrder, request.Details, request.Recover));
	}
}
