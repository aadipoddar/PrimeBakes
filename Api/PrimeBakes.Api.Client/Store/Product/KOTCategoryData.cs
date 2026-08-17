using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Data.Store.Product;

public static class KOTCategoryData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KOTCategoryData));

	public static async Task DeleteTransaction(KOTCategoryModel category, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), category, new { userId, platform });

	public static async Task RecoverTransaction(KOTCategoryModel category, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), category, new { userId, platform });

	public static async Task<int> SaveTransaction(KOTCategoryModel category, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), category, new { userId, platform });
}
