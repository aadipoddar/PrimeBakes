namespace PrimeBakesLibrary.Data;

public static class CustomerData
{
	public static async Task InsertCustomer(CustomerModel customer) =>
		await SqlDataAccess.SaveData("Insert_Customer", new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Status
		});

	public static async Task UpdateCustomer(CustomerModel customer) =>
		await SqlDataAccess.SaveData("Update_Customer", new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Status
		});
}
