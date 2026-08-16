using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Accounts.Masters.Data;

public static class StateUTData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(StateUTData));

	public static async Task DeleteTransaction(StateUTModel state, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), state, new { userId, platform });

	public static async Task RecoverTransaction(StateUTModel state, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), state, new { userId, platform });

	public static async Task<int> SaveTransaction(StateUTModel state, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), state, new { userId, platform });
}
