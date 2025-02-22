namespace PrimeBakesLibrary.Data;

public static class UserCategoryData
{
	public static async Task InsertUserCategory(UserCategoryModel userCategory) =>
	await SqlDataAccess.SaveData(StoredProcedure.InsertUserCategory, new
	{
		userCategory.Id,
		userCategory.Code,
		userCategory.Name,
		userCategory.Status
	});

	public static async Task UpdateUserCategory(UserCategoryModel userCategory) =>
		await SqlDataAccess.SaveData(StoredProcedure.UpdateUserCategory, new
		{
			userCategory.Id,
			userCategory.Code,
			userCategory.Name,
			userCategory.Status
		});
}