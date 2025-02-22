namespace PrimeBakesLibrary.Data;

public static class UserData
{
	public static async Task InsertUser(UserModel user) =>
			await SqlDataAccess.SaveData(StoredProcedure.InsertUser, new
			{
				user.Id,
				user.Name,
				user.Code,
				user.Password,
				user.CustomerId,
				user.UserCategoryId,
				user.Status
			});

	public static async Task UpdateUser(UserModel user) =>
			await SqlDataAccess.SaveData(StoredProcedure.UpdateUser, new
			{
				user.Id,
				user.Name,
				user.Code,
				user.Password,
				user.CustomerId,
				user.UserCategoryId,
				user.Status
			});
}