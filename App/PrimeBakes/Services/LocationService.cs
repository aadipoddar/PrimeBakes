using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class LocationService : ILocationService
{
	public async Task<LocationResult> GetLocationAsync()
	{
		try
		{
			var location = await Geolocation.Default.GetLastKnownLocationAsync()
				?? await Geolocation.Default.GetLocationAsync(
					new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

			return location is null ? null : new()
			{
				Latitude = (decimal)location.Latitude,
				Longitude = (decimal)location.Longitude
			};
		}
		catch { return null; }
	}
}
