using Microsoft.AspNetCore.Mvc;

using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Analysis;

public sealed class GeminiEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GeminiEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(GeminiData.Ask),
			([FromBody] string prompt) => GeminiData.Ask(prompt));
	}
}
