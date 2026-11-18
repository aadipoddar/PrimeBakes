using Microsoft.AspNetCore.Mvc;

using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Analysis;

public sealed class AiEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AiEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(AiData.GeminiAsk),
			([FromBody] string prompt) => AiData.GeminiAsk(prompt));
	}
}
