namespace PrimeBakes.Shared.Services;

public interface ILocationService
{
	public Task<LocationResult> GetLocationAsync();
}

public sealed class LocationResult
{
	public double Latitude { get; set; }
	public double Longitude { get; set; }
}

public static class PlatformInfo
{
	public static async Task<string> GetCreatedFromPlatform(IFormFactor formFactor, ILocationService locationService)
	{
		var platform = $"Form = {formFactor.GetFormFactor()}, Platform = {formFactor.GetPlatform()}";
		var location = await locationService.GetLocationAsync();

		return location is null
			? platform
			: $"{platform}, Lat = {location.Latitude:F6}, Long = {location.Longitude:F6}";
	}
}
