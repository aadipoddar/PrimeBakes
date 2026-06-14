using PrimeBakesLibrary.Common;

namespace PrimeBakesLibrary.Store.Customer;

public static class CustomerData
{
	public static async Task<int> InsertCustomer(CustomerModel customer, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertCustomer, customer, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Customer.");

	public static async Task<CustomerModel> LoadCustomerByNumber(string number) =>
		(await SqlDataAccess.LoadData<CustomerModel, dynamic>(StoreNames.LoadCustomerByNumber, new { Number = number })).FirstOrDefault();
}
