using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Library.Store.Sale.Data;

public static class SaleData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SaleData));

	public static async Task PostDaySales(DateTime postingDate, int locationId, int userId, string userPlatform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(PostDaySales)), new { }, new { postingDate, locationId, userId, userPlatform });

	public static async Task DeleteTransaction(SaleModel sale) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), sale);

	public static async Task RecoverTransaction(SaleModel sale) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), sale);

	public static async Task<int> SaveTransaction(SaleModel sale, List<SaleDetailModel> saleDetails, CustomerModel customer = null, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new SaleSaveRequest(sale, saleDetails, customer, recover));
}
