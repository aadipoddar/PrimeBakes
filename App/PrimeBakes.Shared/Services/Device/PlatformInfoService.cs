namespace PrimeBakes.Shared.Services.Device;

public class PlatformInfoService(IFormFactor formFactor, ILocationService locationService)
{
	public async Task<PlatformInfoModel> GetPlatformInfo()
	{
		var location = await locationService.GetLocationAsync();
		return new()
		{
			FormFactor = formFactor.GetFormFactor(),
			Platform = formFactor.GetPlatform(),
			Latitude = location?.Latitude,
			Longitude = location?.Longitude
		};
	}
}

public sealed class PlatformInfoModel
{
	public string FormFactor { get; set; }
	public string Platform { get; set; }
	public decimal? Latitude { get; set; }
	public decimal? Longitude { get; set; }
}
