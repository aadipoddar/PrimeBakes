using System.Globalization;

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

	public async Task OpenMapAsync(decimal? latitude, decimal? longitude)
	{
		if (latitude is null || longitude is null)
			return;

		try { await Map.Default.OpenAsync((double)latitude.Value, (double)longitude.Value); }
		catch
		{
			await Browser.Default.OpenAsync($"https://www.google.com/maps/search/?api=1&query={latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}", BrowserLaunchMode.External);
		}
	}
}
