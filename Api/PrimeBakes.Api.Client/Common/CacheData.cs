using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Common;

public static class CacheData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(CacheData));

	public static async Task Clear() =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(Clear)), null);
}
