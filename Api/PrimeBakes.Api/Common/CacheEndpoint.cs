using Microsoft.AspNetCore.OutputCaching;

using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Common;

public class CacheEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CacheEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(Clear), Clear);
	}

	private static async Task<int> Clear(IOutputCacheStore store)
	{
		await store.EvictByTagAsync(ApiCachePolicy.Tag, CancellationToken.None);
		await ApiCachePolicy.RefreshTimeout();

		return 1;
	}
}
