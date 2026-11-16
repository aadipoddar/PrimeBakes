using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Analysis;

public sealed class GeminiEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GeminiEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(GeminiData.Ask),
			(string prompt) => GeminiData.Ask(prompt));
	}
}
