using PrimeBakesLibrary.Operations.User.Models;

namespace PrimeBakesLibrary.Operations.User.Data;

public static class UserData
{
    public static async Task InsertUser(UserModel userModel) =>
            await SqlDataAccess.SaveData(OperationNames.InsertUser, userModel);

    public static async Task<UserModel> LoadUserByPasscode(int Passcode) =>
            (await SqlDataAccess.LoadData<UserModel, dynamic>(OperationNames.LoadUserByPasscode, new { Passcode })).FirstOrDefault();
}
