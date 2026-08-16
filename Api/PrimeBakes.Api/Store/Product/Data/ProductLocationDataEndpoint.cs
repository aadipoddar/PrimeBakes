using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Data;

public class ProductLocationDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductLocationDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductLocationData.InsertProductLocation),
			(ProductLocationModel productLocation) => ProductLocationData.InsertProductLocation(productLocation));

		group.MapPost(nameof(ProductLocationData.DeleteProductLocationById),
			(int id) => ProductLocationData.DeleteProductLocationById(id));

		group.MapGet(nameof(ProductLocationData.LoadProductLocationOverviewByProductLocationDate),
			(int? ProductId, int? LocationId, DateOnly? Date) => ProductLocationData.LoadProductLocationOverviewByProductLocationDate(ProductId, LocationId, Date));

		group.MapPost(nameof(ProductLocationData.DeleteTransaction), ProductLocationData.DeleteTransaction);
		group.MapPost(nameof(ProductLocationData.DiscontinueTransaction), ProductLocationData.DiscontinueTransaction);
		group.MapPost(nameof(ProductLocationData.SaveTransaction), ProductLocationData.SaveTransaction);
	}
}
