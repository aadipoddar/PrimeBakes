using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class CompanyData
{
    public static async Task<int> InsertCompany(CompanyModel company) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertCompany, company)).FirstOrDefault();
}
