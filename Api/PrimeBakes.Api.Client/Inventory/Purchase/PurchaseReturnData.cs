using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;

namespace PrimeBakes.Data.Inventory.Purchase;

public static class PurchaseReturnData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PurchaseReturnData));

	public static async Task<PurchaseReturnInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<PurchaseReturnInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });


	public static async Task DeleteTransaction(PurchaseReturnModel purchaseReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), purchaseReturn);

	public static async Task RecoverTransaction(PurchaseReturnModel purchaseReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), purchaseReturn);

	public static async Task<int> SaveTransaction(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new PurchaseReturnSaveRequest(purchaseReturn, purchaseReturnDetails, recover));
}
