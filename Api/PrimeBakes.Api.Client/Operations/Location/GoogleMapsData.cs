using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Data.Operations.Location;

public static class GoogleMapsData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(GoogleMapsData));

	public static async Task<List<PlaceModel>> SearchPlaces(string input) =>
		await ApiClient.Get<List<PlaceModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SearchPlaces)), new { input });
}
