using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Operations.User;

public static class AuthData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AuthData));

	public static async Task<LoginResult> Login(int Passcode, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Get<LoginResult>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Login)), new { Passcode, formFactor, platform, latitude, longitude });
}
