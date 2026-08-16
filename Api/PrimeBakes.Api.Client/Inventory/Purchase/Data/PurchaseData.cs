using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Purchase.Data;

public static class PurchaseData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PurchaseData));

	public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
		await ApiClient.Get<List<RawMaterialModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadRawMaterialByPartyPurchaseDateTime)), new { PartyId, PurchaseDateTime, OnlyActive });

	public static async Task DeleteTransaction(PurchaseModel purchase) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), purchase);

	public static async Task RecoverTransaction(PurchaseModel purchase) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), purchase);

	public static async Task<int> SaveTransaction(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new PurchaseSaveRequest(purchase, purchaseDetails, recover));
}
