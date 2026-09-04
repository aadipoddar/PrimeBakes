using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class SyncData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SyncData));

	public static async Task<string> Backup(int userId) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Backup)), null, new { userId });

	public static async Task<string> SyncToLocalClient() =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SyncToLocalClient)), null);

	public static async Task<DateTime?> LoadLastBackupDate() =>
		await ApiClient.Get<DateTime?>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastBackupDate)));
}
