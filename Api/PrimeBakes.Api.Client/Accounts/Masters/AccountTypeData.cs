using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Accounts.Masters;

public static class AccountTypeData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AccountTypeData));

	public static async Task DeleteTransaction(AccountTypeModel accountType, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), accountType, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(AccountTypeModel accountType, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), accountType, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(AccountTypeModel accountType, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), accountType, new { userId, formFactor, platform, latitude, longitude });
}
