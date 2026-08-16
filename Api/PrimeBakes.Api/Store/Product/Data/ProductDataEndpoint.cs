using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Data;

public class ProductDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductData.InsertProduct), (ProductModel product) => ProductData.InsertProduct(product));
		group.MapPost(nameof(ProductData.DeleteTransaction), ProductData.DeleteTransaction);
		group.MapPost(nameof(ProductData.RecoverTransaction), ProductData.RecoverTransaction);

		group.MapPost(nameof(ProductData.SaveTransaction),
			(ProductSaveRequest request, int userId, string platform) =>
				ProductData.SaveTransaction(request.Product, request.Locations, request.EffectiveDate, userId, platform));
	}
}
