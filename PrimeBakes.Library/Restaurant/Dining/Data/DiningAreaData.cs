using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Restaurant.Dining.Models;

namespace PrimeBakes.Library.Restaurant.Dining.Data;

public static class DiningAreaData
{
	public static async Task<int> InsertDiningArea(DiningAreaModel diningArea, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertDiningArea, diningArea, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Dining Area.");

	public static async Task DeleteTransaction(DiningAreaModel diningArea, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			diningArea.Status = false;
			await InsertDiningArea(diningArea, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = RestaurantNames.DiningArea,
				RecordNo = diningArea.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(DiningAreaModel diningArea, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			diningArea.Status = true;
			await InsertDiningArea(diningArea, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = RestaurantNames.DiningArea,
				RecordNo = diningArea.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(DiningAreaModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Dining area name is required. Please enter a valid name.");

		if (item.LocationId <= 0)
			throw new Exception("Location is required. Please select a location.");

		var allDiningAreas = await CommonData.LoadTableData<DiningAreaModel>(RestaurantNames.DiningArea);

		var existingByName = allDiningAreas.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Dining area name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(DiningAreaModel diningArea, int userId, string platform)
	{
		await ValidateTransaction(diningArea);

		var isUpdate = diningArea.Id > 0;

		var previous = isUpdate
			? await CommonData.LoadTableDataById<DiningAreaModel>(RestaurantNames.DiningArea, diningArea.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertDiningArea(diningArea, transaction);
			var diff = AuditTrailData.GetDifference(previous, diningArea);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = RestaurantNames.DiningArea,
				RecordNo = diningArea.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
