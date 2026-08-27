using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class LocationService(IJSRuntime jsRuntime) : ILocationService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task<LocationResult> GetLocationAsync()
	{
		try { return await _jsRuntime.InvokeAsync<LocationResult>("getCurrentLocation", CancellationToken.None); }
		catch { return null; }
	}
}
