namespace PrimeBakesLibrary.Data;

public static class ItemCategoryData
{
	public static async Task InsertItemCategory(ItemCategoryModel itemCategory) =>
		await SqlDataAccess.SaveData(StoredProcedure.InsertItemCategory, new
		{
			itemCategory.Id,
			itemCategory.Code,
			itemCategory.Name,
			itemCategory.Status
		});

	public static async Task UpdateItemCategory(ItemCategoryModel itemCategory) =>
		await SqlDataAccess.SaveData(StoredProcedure.UpdateItemCategory, new
		{
			itemCategory.Id,
			itemCategory.Code,
			itemCategory.Name,
			itemCategory.Status
		});
}