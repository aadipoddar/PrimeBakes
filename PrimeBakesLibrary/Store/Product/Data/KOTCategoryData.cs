using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class KOTCategoryData
{
	private static async Task<int> InsertKOTCategory(KOTCategoryModel kotCategory, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertKOTCategory, kotCategory, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert KOT Category.");

	public static async Task DeleteTransaction(KOTCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = false;
			await InsertKOTCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.KOTCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(KOTCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = true;
			await InsertKOTCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = StoreNames.KOTCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(KOTCategoryModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Category name is required. Please enter a valid name.");

		var allCategories = await CommonData.LoadTableData<KOTCategoryModel>(StoreNames.KOTCategory);

		var existingByName = allCategories.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"KOT Category name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(KOTCategoryModel category, int userId, string platform)
	{
		await ValidateTransaction(category);

		var isUpdate = category.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<KOTCategoryModel>(StoreNames.KOTCategory, category.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertKOTCategory(category, transaction);
			var diff = AuditTrailData.GetDifference(previous, category);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.KOTCategory,
				RecordNo = category.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
