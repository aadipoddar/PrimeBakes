using PrimeBakes.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Data.Store.Product;

public static class ProductLocationData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductLocationData));

	public static async Task<int> InsertProductLocation(ProductLocationModel productLocation) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertProductLocation)), productLocation);

	public static async Task<int> DeleteProductLocationById(int id) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteProductLocationById)), new { }, new { id });

	public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocationDate(int? ProductId = null, int? LocationId = null, DateOnly? Date = null) =>
		await ApiClient.Get<List<ProductLocationOverviewModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadProductLocationOverviewByProductLocationDate)), new { ProductId, LocationId, Date });

	public static async Task DeleteTransaction(ProductLocationOverviewModel productLocation, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), productLocation, new { userId, platform });

	public static async Task DiscontinueTransaction(ProductLocationOverviewModel productLocation, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DiscontinueTransaction)), productLocation, new { userId, platform });

	public static async Task<int> SaveTransaction(ProductLocationModel productLocation, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), productLocation, new { userId, platform });
}
