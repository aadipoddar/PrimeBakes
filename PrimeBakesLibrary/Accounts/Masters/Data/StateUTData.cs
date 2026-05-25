using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class StateUTData
{
    public static async Task<int> InsertStateUT(StateUTModel state) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertStateUT, state)).FirstOrDefault();
}