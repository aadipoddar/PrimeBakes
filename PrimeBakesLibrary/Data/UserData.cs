namespace PrimeBakesLibrary.Data;

public static class UserData
{
	public static async Task UserInsert(UserModel userModel) =>
			await SqlDataAccess.SaveData("UserInsert", userModel);

	public static async Task UserUpdate(UserModel userModel) =>
			await SqlDataAccess.SaveData("UserUpdate", userModel);
}