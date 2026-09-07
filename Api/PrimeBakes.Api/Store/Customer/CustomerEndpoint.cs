using PrimeBakes.Api.Common;
using PrimeBakes.Data.Store.Customer;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Customer;

public class CustomerEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CustomerEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(CustomerData.LoadCustomerByNumber), (string number) => CustomerData.LoadCustomerByNumber(number));
		group.MapPost(nameof(CustomerData.SaveTransaction), CustomerData.SaveTransaction);
	}
}
