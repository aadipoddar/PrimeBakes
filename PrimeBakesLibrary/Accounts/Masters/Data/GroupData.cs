using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class GroupData
{
    public static async Task<int> InsertGroup(GroupModel group) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertGroup, group)).FirstOrDefault();
}
