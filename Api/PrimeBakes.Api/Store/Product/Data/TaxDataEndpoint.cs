using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product.Data;

public class TaxDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TaxDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(TaxData.DeleteTransaction), TaxData.DeleteTransaction);
		group.MapPost(nameof(TaxData.RecoverTransaction), TaxData.RecoverTransaction);
		group.MapPost(nameof(TaxData.SaveTransaction), TaxData.SaveTransaction);
	}
}
