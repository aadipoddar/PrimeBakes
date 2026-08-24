using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;

namespace PrimeBakes.Data.Inventory.PurchaseOrder;

public static class PurchaseOrderData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PurchaseOrderData));


	public static async Task<List<PurchaseOrderModel>> LoadPurchaseOrderByPartyPending(int PartyId) =>
		await ApiClient.Get<List<PurchaseOrderModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadPurchaseOrderByPartyPending)), new { PartyId });

	public static async Task<PurchaseOrderInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<PurchaseOrderInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });

	public static async Task LinkPurchaseOrderToPurchase(int? purchaseOrderId = null, int? purchaseId = null, bool unlink = false) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LinkPurchaseOrderToPurchase)), new { }, new { purchaseOrderId, purchaseId, unlink });

	public static async Task DeleteTransaction(PurchaseOrderModel purchaseOrder) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), purchaseOrder);

	public static async Task RecoverTransaction(PurchaseOrderModel purchaseOrder) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), purchaseOrder);

	public static async Task<int> SaveTransaction(PurchaseOrderModel purchaseOrder, List<PurchaseOrderDetailModel> purchaseOrderDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new PurchaseOrderSaveRequest(purchaseOrder, purchaseOrderDetails, recover));
}
