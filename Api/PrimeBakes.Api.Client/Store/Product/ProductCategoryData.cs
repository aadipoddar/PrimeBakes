using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Data.Store.Product;

public static class ProductCategoryData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductCategoryData));

	public static async Task DeleteTransaction(ProductCategoryModel category, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), category, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(ProductCategoryModel category, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), category, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(ProductCategoryModel category, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), category, new { userId, formFactor, platform, latitude, longitude });
}
