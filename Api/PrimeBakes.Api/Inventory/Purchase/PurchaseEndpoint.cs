using PrimeBakes.Data.Inventory.Purchase;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Api.Inventory.Purchase;

public class PurchaseEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(PurchaseData.LoadRawMaterialByPartyPurchaseDateTime),
			(int PartyId, DateTime PurchaseDateTime, bool OnlyActive) => PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(PartyId, PurchaseDateTime, OnlyActive));

		group.MapGet(nameof(PurchaseData.LoadInvoiceBundle),
			(int transactionId) => PurchaseData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(PurchaseData.DeleteTransaction), (PurchaseModel purchase) => PurchaseData.DeleteTransaction(purchase));
		group.MapPost(nameof(PurchaseData.RecoverTransaction), (PurchaseModel purchase) => PurchaseData.RecoverTransaction(purchase));
		group.MapPost(nameof(PurchaseData.SaveTransaction), (PurchaseSaveRequest request) => PurchaseData.SaveTransaction(request.Purchase, request.Details, request.Recover));
	}
}
