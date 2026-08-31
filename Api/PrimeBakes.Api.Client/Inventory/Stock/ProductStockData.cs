using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Data.Inventory.Stock;

public static class ProductStockData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductStockData));

	public static async Task<int> DeleteProductStockByTransactionNo(string TransactionNo) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteProductStockByTransactionNo)), new { }, new { TransactionNo });

	public static async Task<List<ProductStockModel>> LoadProductOpeningStockByDateLocationId(DateTime FromDate, int LocationId) =>
		await ApiClient.Get<List<ProductStockModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadProductOpeningStockByDateLocationId)), new { FromDate, LocationId });

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await ApiClient.Get<List<ProductStockSummaryModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadProductStockSummaryByDateLocationId)), new { FromDate, ToDate, LocationId });

	public static async Task DeleteProductStockAdjustment(int id, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteProductStockAdjustment)), new { }, new { id, userId, formFactor, platform, latitude, longitude });

	public static async Task RecalculateStockByDateLocation(DateTime fromDate, DateTime toDate, int locationId, bool deleteAdjustments, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecalculateStockByDateLocation)), new { }, new { fromDate, toDate, locationId, deleteAdjustments, userId, formFactor, platform, latitude, longitude });

	public static async Task SaveProductStockAdjustment(DateTime transactionDateTime, int locationId, List<ProductStockAdjustmentCartModel> cart, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveProductStockAdjustment)),
			new ProductStockAdjustmentRequest(transactionDateTime, locationId, cart), new { userId, formFactor, platform, latitude, longitude });
}
