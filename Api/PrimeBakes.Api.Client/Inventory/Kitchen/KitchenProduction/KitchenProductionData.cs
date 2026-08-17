using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;

public static class KitchenProductionData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenProductionData));

	public static async Task<KitchenProductionInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<KitchenProductionInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });


	public static async Task DeleteTransaction(KitchenProductionModel kitchenProduction) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenProduction);

	public static async Task RecoverTransaction(KitchenProductionModel kitchenProduction) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenProduction);

	public static async Task<int> SaveTransaction(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenProductionSaveRequest(kitchenProduction, kitchenProductionDetails, recover));
}
