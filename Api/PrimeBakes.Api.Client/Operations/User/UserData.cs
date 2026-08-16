using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Library.Operations.User;

public static class UserData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(UserData));

	public static async Task<UserModel> LoadUserByPasscode(int Passcode) =>
		await ApiClient.Get<UserModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadUserByPasscode)), new { Passcode });

	public static async Task DeleteTransaction(UserModel user, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), user, new { userId, platform });

	public static async Task RecoverTransaction(UserModel user, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), user, new { userId, platform });

	public static async Task<int> SaveTransaction(UserModel user, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), user, new { userId, platform });
}
