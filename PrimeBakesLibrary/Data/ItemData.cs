namespace PrimeBakesLibrary.Data;

public static class ItemData
{
	public static async Task InsertItem(ItemModel item) =>
		await SqlDataAccess.SaveData("Insert_Item", new
		{
			item.Id,
			item.CategoryId,
			item.Code,
			item.Name,
			item.Status
		});

	public static async Task UpdateItem(ItemModel item) =>
		await SqlDataAccess.SaveData("Update_Item", new
		{
			item.Id,
			item.CategoryId,
			item.Code,
			item.Name,
			item.Status
		});
}