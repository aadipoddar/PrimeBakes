using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Accounts.Masters;

public static class LedgerData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(LedgerData));

	public static async Task DeleteTransaction(LedgerModel ledger, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), ledger, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(LedgerModel ledger, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), ledger, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(LedgerModel ledger, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), ledger, new { userId, formFactor, platform, latitude, longitude });
}
