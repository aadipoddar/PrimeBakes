using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class ProductCategoryData
{
	private static async Task<int> InsertProductCategory(ProductCategoryModel productCategory, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProductCategory, productCategory, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Category.");

	public static async Task DeleteTransaction(ProductCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = false;
			await InsertProductCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.ProductCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(ProductCategoryModel category, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			category.Status = true;
			await InsertProductCategory(category, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = StoreNames.ProductCategory,
				RecordNo = category.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(ProductCategoryModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Category name is required. Please enter a valid name.");

		var allCategories = await CommonData.LoadTableData<ProductCategoryModel>(StoreNames.ProductCategory);

		var existingByName = allCategories.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Product Category name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(ProductCategoryModel category, int userId, string platform)
	{
		await ValidateTransaction(category);

		var isUpdate = category.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<ProductCategoryModel>(StoreNames.ProductCategory, category.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertProductCategory(category, transaction);
			var diff = AuditTrailData.GetDifference(previous, category);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.ProductCategory,
				RecordNo = category.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
