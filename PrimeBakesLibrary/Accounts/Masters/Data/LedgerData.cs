using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class LedgerData
{
    public static async Task<int> InsertLedger(LedgerModel ledger, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertLedger, ledger, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<LedgerModel> LoadLedgerByLocationId(int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId, sqlDataAccessTransaction);
        return await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, location.LedgerId, sqlDataAccessTransaction);
    }
}
