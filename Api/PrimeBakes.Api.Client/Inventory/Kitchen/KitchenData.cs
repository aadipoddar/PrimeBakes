using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Data.Inventory.Kitchen;

public static class KitchenData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenData));

	public static async Task DeleteTransaction(KitchenModel kitchen, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchen, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(KitchenModel kitchen, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchen, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(KitchenModel kitchen, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), kitchen, new { userId, formFactor, platform, latitude, longitude });
}
