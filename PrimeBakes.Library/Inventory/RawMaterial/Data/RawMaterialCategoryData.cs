using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.RawMaterial.Models;
using PrimeBakes.Library.Operations.AuditTrail;

namespace PrimeBakes.Library.Inventory.RawMaterial.Data;

public static class RawMaterialCategoryData
{
	private static async Task<int> InsertRawMaterialCategory(RawMaterialCategoryModel rawMaterialCategory, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertRawMaterialCategory, rawMaterialCategory, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Raw Material Category.");

	public static async Task DeleteTransaction(RawMaterialCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = false;
			await InsertRawMaterialCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = InventoryNames.RawMaterialCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(RawMaterialCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = true;
			await InsertRawMaterialCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = InventoryNames.RawMaterialCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(RawMaterialCategoryModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Category name is required. Please enter a valid name.");

		var allCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);

		var existingByName = allCategories.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Raw Material Category name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(RawMaterialCategoryModel category, int userId, string platform)
	{
		await ValidateTransaction(category);

		var isUpdate = category.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory, category.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertRawMaterialCategory(category, transaction);
			var diff = AuditTrailData.GetDifference(previous, category);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = InventoryNames.RawMaterialCategory,
				RecordNo = category.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}