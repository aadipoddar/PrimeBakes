using PrimeBakes.Library.Restaurant.Dining.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Api.Restaurant.Dining.Data;

public class DiningAreaDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DiningAreaDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DiningAreaData.InsertDiningArea), (DiningAreaModel diningArea) => DiningAreaData.InsertDiningArea(diningArea));
		group.MapPost(nameof(DiningAreaData.DeleteTransaction), DiningAreaData.DeleteTransaction);
		group.MapPost(nameof(DiningAreaData.RecoverTransaction), DiningAreaData.RecoverTransaction);
		group.MapPost(nameof(DiningAreaData.SaveTransaction), DiningAreaData.SaveTransaction);
	}
}
