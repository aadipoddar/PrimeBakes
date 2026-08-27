using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Data.Restaurant.Dining;

public static class DiningAreaData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DiningAreaData));

	public static async Task<int> InsertDiningArea(DiningAreaModel diningArea) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertDiningArea)), diningArea);

	public static async Task DeleteTransaction(DiningAreaModel diningArea, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), diningArea, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(DiningAreaModel diningArea, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), diningArea, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(DiningAreaModel diningArea, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), diningArea, new { userId, formFactor, platform, latitude, longitude });
}
