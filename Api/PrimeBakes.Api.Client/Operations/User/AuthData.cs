using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Operations.User;

public static class AuthData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AuthData));

	public static async Task<LoginResult> Login(int Passcode) =>
		await ApiClient.Get<LoginResult>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Login)), new { Passcode });
}
