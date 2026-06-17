using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Restaurant.Dining.Models;

namespace PrimeBakesLibrary.Restaurant.Dining.Data;

public static class DiningTableData
{
	public static async Task<int> InsertDiningTable(DiningTableModel diningTable, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertDiningTable, diningTable, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Dining Table.");

	public static async Task UpdateLayouts(List<DiningTableModel> diningTables) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var diningTable in diningTables)
				await InsertDiningTable(diningTable, transaction);
		});

	public static async Task DeleteTransaction(DiningTableModel diningTable, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			diningTable.Status = false;
			await InsertDiningTable(diningTable, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = RestaurantNames.DiningTable,
				RecordNo = diningTable.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(DiningTableModel diningTable, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			diningTable.Status = true;
			await InsertDiningTable(diningTable, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = RestaurantNames.DiningTable,
				RecordNo = diningTable.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(DiningTableModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Dining table name is required. Please enter a valid name.");

		if (item.DiningAreaId <= 0)
			throw new Exception("Dining area is required. Please select a dining area.");

		var allDiningTables = await CommonData.LoadTableData<DiningTableModel>(RestaurantNames.DiningTable);

		var existingByName = allDiningTables.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Dining table name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(DiningTableModel diningTable, int userId, string platform)
	{
		await ValidateTransaction(diningTable);

		var isUpdate = diningTable.Id > 0;

		var previous = isUpdate
			? await CommonData.LoadTableDataById<DiningTableModel>(RestaurantNames.DiningTable, diningTable.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertDiningTable(diningTable, transaction);
			var diff = AuditTrailData.GetDifference(previous, diningTable);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = RestaurantNames.DiningTable,
				RecordNo = diningTable.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
