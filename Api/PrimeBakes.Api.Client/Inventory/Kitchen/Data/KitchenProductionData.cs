using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Library.Inventory.Kitchen.Data;

public static class KitchenProductionData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenProductionData));

	public static async Task DeleteTransaction(KitchenProductionModel kitchenProduction) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenProduction);

	public static async Task RecoverTransaction(KitchenProductionModel kitchenProduction) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenProduction);

	public static async Task<int> SaveTransaction(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenProductionSaveRequest(kitchenProduction, kitchenProductionDetails, recover));
}
