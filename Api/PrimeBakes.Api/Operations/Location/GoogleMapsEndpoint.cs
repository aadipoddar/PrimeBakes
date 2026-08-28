using PrimeBakes.Data.Operations.Location;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Location;

public class GoogleMapsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GoogleMapsEndpoint));
		app.MapGroup(endpoint).WithTags(endpoint).MapGet(nameof(GoogleMapsData.SearchPlaces), GoogleMapsData.SearchPlaces);
	}
}
