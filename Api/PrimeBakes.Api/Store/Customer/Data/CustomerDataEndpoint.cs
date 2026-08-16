using PrimeBakes.Library.Store.Customer.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Api.Store.Customer.Data;

public class CustomerDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CustomerDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CustomerData.InsertCustomer), (CustomerModel customer) => CustomerData.InsertCustomer(customer));
		group.MapGet(nameof(CustomerData.LoadCustomerByNumber), (string number) => CustomerData.LoadCustomerByNumber(number));
		group.MapPost(nameof(CustomerData.SaveTransaction), CustomerData.SaveTransaction);
	}
}
