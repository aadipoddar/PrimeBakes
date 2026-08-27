using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Data.Store.Product;

public static class ProductData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductData));

	public static async Task<int> InsertProduct(ProductModel product) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertProduct)), product);

	public static async Task DeleteTransaction(ProductModel product, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), product, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(ProductModel product, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), product, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(ProductModel product, List<LocationModel> locations, DateOnly effectiveDate, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new ProductSaveRequest(product, locations, effectiveDate), new { userId, formFactor, platform, latitude, longitude });
}
