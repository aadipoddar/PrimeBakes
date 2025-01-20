namespace PrimeBakesLibrary.Data;

public static class CategoryData
{
	public static async Task InsertCategory(CategoryModel category) =>
		await SqlDataAccess.SaveData(StoredProcedure.InsertCategory, new
		{
			category.Id,
			category.Code,
			category.Name,
			category.Status
		});

	public static async Task UpdateCategory(CategoryModel category) =>
		await SqlDataAccess.SaveData(StoredProcedure.UpdateCategory, new
		{
			category.Id,
			category.Code,
			category.Name,
			category.Status
		});
}