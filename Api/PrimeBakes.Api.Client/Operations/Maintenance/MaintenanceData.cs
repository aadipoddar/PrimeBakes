using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Maintenance;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class MaintenanceData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(MaintenanceData));

	public static async Task RebuildIndexes() =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RebuildIndexes)), null);

	public static async Task<DatabaseSizeModel> LoadDatabaseSize() =>
		await ApiClient.Get<DatabaseSizeModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadDatabaseSize)));
}
