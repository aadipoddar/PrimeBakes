using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Data.Store.Customer;

public static class OnlinePartnerData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(OnlinePartnerData));

	public static async Task DeleteTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), onlinePartner, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), onlinePartner, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), onlinePartner, new { userId, formFactor, platform, latitude, longitude });
}
