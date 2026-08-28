using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Operations.User;

public static class UserData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(UserData));

	public static async Task<UserModel> LoadUserByPasscode(int Passcode) =>
		await ApiClient.Get<UserModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadUserByPasscode)), new { Passcode });

	public static async Task UpdateLastLoginTime(UserModel user, DateTime? lastLoginTime) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(UpdateLastLoginTime)), user, new { lastLoginTime });

	public static async Task UpdateLastSeen(UserModel user, DateTime lastSeen) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(UpdateLastSeen)), user, new { lastSeen });

	public static async Task DeleteTransaction(UserModel user, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), user, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(UserModel user, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), user, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(UserModel user, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), user, new { userId, formFactor, platform, latitude, longitude });
}
