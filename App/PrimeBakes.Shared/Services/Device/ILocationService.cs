namespace PrimeBakes.Shared.Services.Device;

public interface ILocationService
{
	public Task<LocationResult> GetLocationAsync();

	public Task OpenMapAsync(decimal? latitude, decimal? longitude);
}

public sealed class LocationResult
{
	public decimal Latitude { get; set; }
	public decimal Longitude { get; set; }
}
