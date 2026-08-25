using PrimeBakes.Api.Common;
using PrimeBakes.Data.Store.Customer;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Api.Store.Customer;

public class CustomerEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CustomerEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapPost(nameof(CustomerData.InsertCustomer), (CustomerModel customer) => CustomerData.InsertCustomer(customer));
		group.MapGet(nameof(CustomerData.LoadCustomerByNumber), (string number) => CustomerData.LoadCustomerByNumber(number));
		group.MapPost(nameof(CustomerData.SaveTransaction), CustomerData.SaveTransaction);
	}
}
