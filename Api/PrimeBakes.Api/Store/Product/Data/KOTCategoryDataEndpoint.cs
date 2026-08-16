using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product.Data;

public class KOTCategoryDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KOTCategoryDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KOTCategoryData.DeleteTransaction), KOTCategoryData.DeleteTransaction);
		group.MapPost(nameof(KOTCategoryData.RecoverTransaction), KOTCategoryData.RecoverTransaction);
		group.MapPost(nameof(KOTCategoryData.SaveTransaction), KOTCategoryData.SaveTransaction);
	}
}
