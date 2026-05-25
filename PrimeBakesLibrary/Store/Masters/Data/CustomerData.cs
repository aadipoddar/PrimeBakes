using PrimeBakesLibrary.Store.Masters.Models;

namespace PrimeBakesLibrary.Store.Masters.Data;

public static class CustomerData
{
	public static async Task<int> InsertCustomer(CustomerModel customer) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertCustomer, customer)).FirstOrDefault();

	public static async Task<CustomerModel> LoadCustomerByNumber(string number) =>
			(await SqlDataAccess.LoadData<CustomerModel, dynamic>(StoreNames.LoadCustomerByNumber, new { Number = number })).FirstOrDefault();
}
