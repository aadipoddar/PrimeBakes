using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;

public static class KitchenProductionReturnData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReturnData));

	public static async Task<KitchenProductionReturnInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<KitchenProductionReturnInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });


	public static async Task DeleteTransaction(KitchenProductionReturnModel kitchenProductionReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenProductionReturn);

	public static async Task RecoverTransaction(KitchenProductionReturnModel kitchenProductionReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenProductionReturn);

	public static async Task<int> SaveTransaction(KitchenProductionReturnModel kitchenProductionReturn, List<KitchenProductionReturnDetailModel> kitchenProductionReturnDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenProductionReturnSaveRequest(kitchenProductionReturn, kitchenProductionReturnDetails, recover));
}
