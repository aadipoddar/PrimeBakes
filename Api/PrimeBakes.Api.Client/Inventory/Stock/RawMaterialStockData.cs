using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Data.Inventory.Stock;

public static class RawMaterialStockData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialStockData));

	public static async Task<int> InsertRawMaterialStock(RawMaterialStockModel stock) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertRawMaterialStock)), stock);

	public static async Task<int> DeleteRawMaterialStockByTransactionNo(string TransactionNo) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteRawMaterialStockByTransactionNo)), new { }, new { TransactionNo });

	public static async Task<List<RawMaterialStockModel>> LoadRawMaterialOpeningStockByDate(DateTime FromDate) =>
		await ApiClient.Get<List<RawMaterialStockModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadRawMaterialOpeningStockByDate)), new { FromDate });

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate) =>
		await ApiClient.Get<List<RawMaterialStockSummaryModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadRawMaterialStockSummaryByDate)), new { FromDate, ToDate });

	public static async Task DeleteRawMaterialStockAdjustment(int id, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteRawMaterialStockAdjustment)), new { }, new { id, userId, formFactor, platform, latitude, longitude });

	public static async Task RecalculateStockByDate(DateTime fromDate, DateTime toDate, bool deleteAdjustments, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecalculateStockByDate)), new { }, new { fromDate, toDate, deleteAdjustments, userId, formFactor, platform, latitude, longitude });

	public static async Task SaveRawMaterialStockAdjustment(DateTime transactionDateTime, List<RawMaterialStockAdjustmentCartModel> cart, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveRawMaterialStockAdjustment)),
			new RawMaterialStockAdjustmentRequest(transactionDateTime, cart), new { userId, formFactor, platform, latitude, longitude });
}
