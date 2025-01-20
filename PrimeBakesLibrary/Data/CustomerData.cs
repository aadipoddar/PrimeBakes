namespace PrimeBakesLibrary.Data;

public static class CustomerData
{
	public static async Task InsertCustomer(CustomerModel customer) =>
		await SqlDataAccess.SaveData(StoredProcedure.InsertCustomer, new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Email,
			customer.Status
		});

	public static async Task UpdateCustomer(CustomerModel customer) =>
		await SqlDataAccess.SaveData(StoredProcedure.UpdateCustomer, new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Email,
			customer.Status
		});
}
