namespace PrimeBakesLibrary.Data;

public static class CustomerData
{
	public static async Task CustomerInsert(CustomerModel customer)
	{
		await SqlDataAccess.SaveData("CustomerInsert", new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Status
		});
	}

	public static async Task CustomerUpdate(CustomerModel customer)
	{
		await SqlDataAccess.SaveData("CustomerUpdate", new
		{
			customer.Id,
			customer.Code,
			customer.Name,
			customer.Status
		});
	}
}
