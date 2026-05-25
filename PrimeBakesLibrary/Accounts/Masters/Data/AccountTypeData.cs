using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class AccountTypeData
{
    public static async Task<int> InsertAccountType(AccountTypeModel accountType) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertAccountType, accountType)).FirstOrDefault();
}
