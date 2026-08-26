using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Operations.Settings;

public static class MaintenanceData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(MaintenanceData));

	public static async Task RebuildIndexes() =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RebuildIndexes)), null);
}
