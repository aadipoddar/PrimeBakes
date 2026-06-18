using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Store.Customer.Models;

namespace PrimeBakesLibrary.Store.Customer.Data;

public static class CustomerData
{
	public static async Task<int> InsertCustomer(CustomerModel customer, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertCustomer, customer, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Customer.");

	public static async Task<CustomerModel> LoadCustomerByNumber(string number) =>
		(await SqlDataAccess.LoadData<CustomerModel, dynamic>(StoreNames.LoadCustomerByNumber, new { Number = number })).FirstOrDefault();

	private static async Task ValidateTransaction(CustomerModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Customer name is required. Please enter a valid name.");

		if (!Helper.ValidatePhoneNumber(item.Number))
			throw new Exception($"Customer number '{item.Number}' is invalid. Please enter a valid phone number.");

		var allCustomers = await CommonData.LoadTableData<CustomerModel>(StoreNames.Customer);

		var existingByName = allCustomers.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Customer name '{item.Name}' already exists. Please choose a different name.");

		var existingByNumber = allCustomers.FirstOrDefault(x => x.Id != item.Id && x.Number.Equals(item.Number, StringComparison.OrdinalIgnoreCase));
		if (existingByNumber is not null)
			throw new Exception($"Customer number '{item.Number}' already exists. Please choose a different number.");
	}

	public static async Task<int> SaveTransaction(CustomerModel customer, int userId, string platform)
	{
		await ValidateTransaction(customer);

		var isUpdate = customer.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<CustomerModel>(StoreNames.Customer, customer.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertCustomer(customer, transaction);
			var diff = AuditTrailData.GetDifference(previous, customer);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.Customer,
				RecordNo = customer.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
