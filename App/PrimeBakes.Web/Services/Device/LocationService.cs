using System.Globalization;

using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Web.Services.Device;

public class LocationService(IJSRuntime jsRuntime) : ILocationService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task<LocationResult> GetLocationAsync()
	{
		try { return await _jsRuntime.InvokeAsync<LocationResult>("getCurrentLocation", CancellationToken.None); }
		catch { return null; }
	}

	public async Task OpenMapAsync(decimal? latitude, decimal? longitude)
	{
		if (latitude is null || longitude is null)
			return;

		await _jsRuntime.InvokeVoidAsync("open", $"https://www.google.com/maps/search/?api=1&query={latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}", "_blank");
	}
}
