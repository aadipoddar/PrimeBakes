using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Accounts.Masters;

public static class GroupData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(GroupData));

	public static async Task DeleteTransaction(GroupModel group, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), group, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(GroupModel group, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), group, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(GroupModel group, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), group, new { userId, formFactor, platform, latitude, longitude });
}
