namespace PrimeBakesLibrary.Data;

public static class UserData
{
	public static async Task InsertUser(UserModel userModel) =>
			await SqlDataAccess.SaveData(StoredProcedure.InsertUser, userModel);

	public static async Task UpdateUser(UserModel userModel) =>
			await SqlDataAccess.SaveData(StoredProcedure.UpdateUser, userModel);
}