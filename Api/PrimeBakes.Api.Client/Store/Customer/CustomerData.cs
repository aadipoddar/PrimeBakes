using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Data.Store.Customer;

public static class CustomerData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(CustomerData));

	public static async Task<int> InsertCustomer(CustomerModel customer) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertCustomer)), customer);

	public static async Task<CustomerModel> LoadCustomerByNumber(string number) =>
		await ApiClient.Get<CustomerModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadCustomerByNumber)), new { number });

	public static async Task<int> SaveTransaction(CustomerModel customer, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), customer, new { userId, formFactor, platform, latitude, longitude });
}
