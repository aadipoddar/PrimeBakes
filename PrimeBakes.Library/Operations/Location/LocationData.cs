using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;

namespace PrimeBakes.Library.Operations.Location;

public static class LocationData
{
	private static async Task<int> InsertLocation(LocationModel location, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertLocation, location, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Location.");

	public static async Task<LocationModel?> LoadLocationByLedgerId(int ledgerId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location, true, sqlDataAccessTransaction);
		return locations.FirstOrDefault(l => l.LedgerId == ledgerId);
	}

	public static async Task<LedgerModel> LoadLedgerByLocationId(int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId, sqlDataAccessTransaction);
		return await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, location.LedgerId, sqlDataAccessTransaction);
	}

	public static async Task DeleteTransaction(LocationModel location, int userId, string platform)
	{
		if (location.Id == 1)
			throw new Exception("Cannot delete the main location.");

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = false;
			await InsertLocation(location, transaction);

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(LocationId: location.Id, sqlDataAccessTransaction: transaction);
			foreach (var productLocation in productLocations)
				await ProductLocationData.DeleteProductLocationById(productLocation.Id, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = OperationNames.Location,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
	}

	public static async Task RecoverTransaction(LocationModel location, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			location.Status = true;
			await InsertLocation(location, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = OperationNames.Location,
				RecordNo = location.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(LocationModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper().RemoveSpace() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Location name is required. Please enter a valid location name.");

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Location code is required. Please enter a valid location code.");

		if (item.LedgerId <= 0)
			throw new Exception("Ledger is required. Please select a valid ledger.");

		if (item.Discount is < 0 or > 100)
			throw new Exception("Discount must be between 0% and 100%. Please enter a valid discount.");

		var allLocations = await CommonData.LoadTableData<LocationModel>(OperationNames.Location);

		var existingByName = allLocations.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Location name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allLocations.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Location code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(LocationModel location, LocationModel copyLocation, int userId, string platform)
	{
		await ValidateTransaction(location);

		var isUpdate = location.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, location.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			bool isNewLocation = location.Id == 0;

			location.Id = await InsertLocation(location, transaction);
			await InsertProducts(location, copyLocation, isNewLocation, transaction);

			var diff = AuditTrailData.GetDifference(previous, location);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = OperationNames.Location,
				RecordNo = location.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);

			return location.Id;
		});
	}

	private static async Task InsertProducts(LocationModel location, LocationModel copyLocation, bool isNewLocation, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (copyLocation is not null && copyLocation.Id > 0)
		{
			if (copyLocation.Id == location.Id)
				return;

			var existingProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(LocationId: location.Id, sqlDataAccessTransaction: sqlDataAccessTransaction);
			foreach (var existingProductLocation in existingProductLocations)
				await ProductLocationData.DeleteProductLocationById(existingProductLocation.Id, sqlDataAccessTransaction);

			var copyProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(LocationId: copyLocation.Id, sqlDataAccessTransaction: sqlDataAccessTransaction);
			foreach (var copyProductLocation in copyProductLocations)
				await ProductLocationData.InsertProductLocation(new()
				{
					Id = 0,
					ProductId = copyProductLocation.ProductId,
					LocationId = location.Id,
					Rate = copyProductLocation.Rate,
					FromDate = copyProductLocation.FromDate
				}, sqlDataAccessTransaction);
		}

		else if (isNewLocation)
		{
			var existingProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(LocationId: location.Id, sqlDataAccessTransaction: sqlDataAccessTransaction);
			foreach (var existingProductLocation in existingProductLocations)
				await ProductLocationData.DeleteProductLocationById(existingProductLocation.Id, sqlDataAccessTransaction);

			var products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product, true, sqlDataAccessTransaction);
			var currentDateTime = await CommonData.LoadCurrentDateTime(sqlDataAccessTransaction);
			foreach (var product in products)
				await ProductLocationData.InsertProductLocation(new()
				{
					Id = 0,
					ProductId = product.Id,
					LocationId = location.Id,
					Rate = product.Rate,
					FromDate = DateOnly.FromDateTime(currentDateTime)
				}, sqlDataAccessTransaction);
		}
	}
}
