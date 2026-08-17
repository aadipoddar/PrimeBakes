using PrimeBakes.Data.Store.Product;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Product;

public class TaxEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TaxEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(TaxData.DeleteTransaction), TaxData.DeleteTransaction);
		group.MapPost(nameof(TaxData.RecoverTransaction), TaxData.RecoverTransaction);
		group.MapPost(nameof(TaxData.SaveTransaction), TaxData.SaveTransaction);
	}
}
