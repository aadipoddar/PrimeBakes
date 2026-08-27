namespace PrimeBakes.Shared.Services;

public sealed class PlatformInfoModel
{
	public string FormFactor { get; set; }
	public string Platform { get; set; }
	public decimal? Latitude { get; set; }
	public decimal? Longitude { get; set; }
}

public static class PlatformInfo
{
	public static async Task<PlatformInfoModel> GetPlatformInfo(IFormFactor formFactor, ILocationService locationService)
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
