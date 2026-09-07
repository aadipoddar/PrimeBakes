using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Data.Store.Customer;

public static class CustomerData
{
	internal static async Task<int> InsertCustomer(CustomerModel customer, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertCustomer, customer, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Customer.");

	public static async Task<CustomerModel> LoadCustomerByNumber(string number, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<CustomerModel, dynamic>(StoreNames.LoadCustomerByNumber, new { Number = number }, sqlDataAccessTransaction)).FirstOrDefault();

	internal static async Task<int?> ResolveCustomer(CustomerModel customer, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (customer is null || customer.Id > 0)
			return customer?.Id is > 0 ? customer.Id : null;

		customer.Number = string.IsNullOrWhiteSpace(customer.Number) ? null : customer.Number.Trim();
		customer.Name = string.IsNullOrWhiteSpace(customer.Name) ? null : customer.Name.Trim();

		if (customer.Number is null)
			return null;

		var existingCustomer = await LoadCustomerByNumber(customer.Number, sqlDataAccessTransaction);
		if (existingCustomer is not null && existingCustomer.Id > 0)
			return existingCustomer.Id;

		if (customer.Name is null)
			throw new InvalidOperationException("Please enter a name for the new customer or clear the customer field.");

		if (!Helper.ValidatePhoneNumber(customer.Number))
			throw new InvalidOperationException("Please enter a valid phone number for the new customer.");

		return await InsertCustomer(customer, sqlDataAccessTransaction);
	}

	private static async Task ValidateTransaction(CustomerModel item)
	{
		if (OfflineState.Offline)
			throw new Exception("Masters cannot be changed while offline.");

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

	public static async Task<int> SaveTransaction(CustomerModel customer, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
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
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
			return id;
		});
	}
}
