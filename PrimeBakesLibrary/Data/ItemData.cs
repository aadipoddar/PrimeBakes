namespace PrimeBakesLibrary.Data;

public static class ItemData
{
	public static async Task InsertItem(ItemModel item) =>
		await SqlDataAccess.SaveData(StoredProcedure.InsertItem, new
		{
			item.Id,
			item.CategoryId,
			item.Code,
			item.Name,
			item.Status
		});

	public static async Task UpdateItem(ItemModel item) =>
		await SqlDataAccess.SaveData(StoredProcedure.UpdateItem, new
		{
			item.Id,
			item.CategoryId,
			item.Code,
			item.Name,
			item.Status
		});

	public static async Task<List<ItemModel>> LoadItemByCategory(int CategoryId) =>
		await SqlDataAccess.LoadData<ItemModel, dynamic>(StoredProcedure.LoadActiveItemsByCategory, new { CategoryId });
}