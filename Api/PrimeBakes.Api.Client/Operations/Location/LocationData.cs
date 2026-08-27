using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Data.Operations.Location;

public static class LocationData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(LocationData));

	public static async Task<LedgerModel> LoadLedgerByLocationId(int locationId) =>
		await ApiClient.Get<LedgerModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLedgerByLocationId)), new { locationId });

	public static async Task DeleteTransaction(LocationModel location, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), location, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(LocationModel location, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), location, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(LocationModel location, LocationModel copyLocation, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new LocationSaveRequest(location, copyLocation), new { userId, formFactor, platform, latitude, longitude });
}
