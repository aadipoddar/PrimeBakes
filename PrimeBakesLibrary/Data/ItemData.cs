namespace PrimeBakesLibrary.Data;

public class ItemData
{
	public static async Task ItemInsert(ItemModel item)
	{
		await SqlDataAccess.SaveData("ItemInsert", new
		{
			item.Id,
			item.Code,
			item.Name,
			item.Status
		});
	}

	public static async Task ItemUpdate(ItemModel item)
	{
		await SqlDataAccess.SaveData("ItemUpdate", new
		{
			item.Id,
			item.Code,
			item.Name,
			item.Status
		});
	}
}