using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Operations.AuditTrail;

namespace PrimeBakesLibrary.Inventory.Kitchen.Data;

public static class KitchenData
{
	private static async Task<int> InsertKitchen(KitchenModel kitchen, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchen, kitchen, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen.");

	public static async Task DeleteTransaction(KitchenModel kitchen, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			kitchen.Status = false;
			await InsertKitchen(kitchen, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = InventoryNames.Kitchen,
				RecordNo = kitchen.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(KitchenModel kitchen, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			kitchen.Status = true;
			await InsertKitchen(kitchen, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = InventoryNames.Kitchen,
				RecordNo = kitchen.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(KitchenModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Kitchen name is required. Please enter a valid kitchen name.");

		var allKitchens = await CommonData.LoadTableData<KitchenModel>(InventoryNames.Kitchen);

		var existingByName = allKitchens.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Kitchen name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(KitchenModel kitchen, int userId, string platform)
	{
		await ValidateTransaction(kitchen);

		var isUpdate = kitchen.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<KitchenModel>(InventoryNames.Kitchen, kitchen.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertKitchen(kitchen, transaction);
			var diff = AuditTrailData.GetDifference(previous, kitchen);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = InventoryNames.Kitchen,
				RecordNo = kitchen.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
