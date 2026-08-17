using PrimeBakes.Data.Store.Product;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product;

public class KOTCategoryEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KOTCategoryEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KOTCategoryData.DeleteTransaction), KOTCategoryData.DeleteTransaction);
		group.MapPost(nameof(KOTCategoryData.RecoverTransaction), KOTCategoryData.RecoverTransaction);
		group.MapPost(nameof(KOTCategoryData.SaveTransaction), KOTCategoryData.SaveTransaction);
	}
}
