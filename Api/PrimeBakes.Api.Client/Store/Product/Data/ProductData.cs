using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Data;

public static class ProductData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductData));

	public static async Task<int> InsertProduct(ProductModel product) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertProduct)), product);

	public static async Task DeleteTransaction(ProductModel product, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), product, new { userId, platform });

	public static async Task RecoverTransaction(ProductModel product, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), product, new { userId, platform });

	public static async Task<int> SaveTransaction(ProductModel product, List<LocationModel> locations, DateOnly effectiveDate, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new ProductSaveRequest(product, locations, effectiveDate), new { userId, platform });
}
