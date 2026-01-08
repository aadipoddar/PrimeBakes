using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class LedgerData
{
    public static async Task<int> InsertLedger(LedgerModel ledger, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertLedger, ledger, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<LedgerModel> LoadLedgerByLocationId(int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId, sqlDataAccessTransaction);
        return await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, location.LedgerId, sqlDataAccessTransaction);
    }
}
