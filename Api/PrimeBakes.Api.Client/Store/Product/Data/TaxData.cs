using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Product.Data;

public static class TaxData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(TaxData));

	public static async Task DeleteTransaction(TaxModel tax, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), tax, new { userId, platform });

	public static async Task RecoverTransaction(TaxModel tax, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), tax, new { userId, platform });

	public static async Task<int> SaveTransaction(TaxModel tax, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), tax, new { userId, platform });
}
