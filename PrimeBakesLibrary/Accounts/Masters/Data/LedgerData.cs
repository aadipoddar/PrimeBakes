using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

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
