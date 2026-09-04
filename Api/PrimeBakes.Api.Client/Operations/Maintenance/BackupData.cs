using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class BackupData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BackupData));

	public static async Task<string> Backup(int userId) =>
		await ApiClient.Post<string>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Backup)), null, new { userId });

	public static async Task<DateTime?> LoadLastBackupDate() =>
		await ApiClient.Get<DateTime?>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastBackupDate)));
}
