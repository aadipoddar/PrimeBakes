namespace PrimeBakes.Shared.Services;

public interface ILocationService
{
	public Task<LocationResult> GetLocationAsync();
}

public sealed class LocationResult
{
	public decimal Latitude { get; set; }
	public decimal Longitude { get; set; }
}
