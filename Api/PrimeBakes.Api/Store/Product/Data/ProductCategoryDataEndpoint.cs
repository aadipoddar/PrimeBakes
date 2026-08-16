using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product.Data;

public class ProductCategoryDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductCategoryDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductCategoryData.DeleteTransaction), ProductCategoryData.DeleteTransaction);
		group.MapPost(nameof(ProductCategoryData.RecoverTransaction), ProductCategoryData.RecoverTransaction);
		group.MapPost(nameof(ProductCategoryData.SaveTransaction), ProductCategoryData.SaveTransaction);
	}
}
