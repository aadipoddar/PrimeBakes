using PrimeBakes.Library.Inventory.Purchase.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Api.Inventory.Purchase.Data;

public class PurchaseDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PurchaseDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(PurchaseData.LoadRawMaterialByPartyPurchaseDateTime),
			(int PartyId, DateTime PurchaseDateTime, bool OnlyActive) => PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(PartyId, PurchaseDateTime, OnlyActive));

		group.MapPost(nameof(PurchaseData.DeleteTransaction), (PurchaseModel purchase) => PurchaseData.DeleteTransaction(purchase));
		group.MapPost(nameof(PurchaseData.RecoverTransaction), (PurchaseModel purchase) => PurchaseData.RecoverTransaction(purchase));
		group.MapPost(nameof(PurchaseData.SaveTransaction), (PurchaseSaveRequest request) => PurchaseData.SaveTransaction(request.Purchase, request.Details, request.Recover));
	}
}
