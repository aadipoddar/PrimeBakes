using PrimeBakes.Data.Store.Product;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product;

public class ProductCategoryEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductCategoryEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductCategoryData.DeleteTransaction), ProductCategoryData.DeleteTransaction);
		group.MapPost(nameof(ProductCategoryData.RecoverTransaction), ProductCategoryData.RecoverTransaction);
		group.MapPost(nameof(ProductCategoryData.SaveTransaction), ProductCategoryData.SaveTransaction);
	}
}
