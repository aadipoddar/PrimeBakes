namespace PrimeBakesLibrary.Data;

public static class UserData
{
	public static async Task InsertUser(UserModel userModel) =>
			await SqlDataAccess.SaveData("Insert_User", userModel);

	public static async Task UpdateUser(UserModel userModel) =>
			await SqlDataAccess.SaveData("Update_User", userModel);
}