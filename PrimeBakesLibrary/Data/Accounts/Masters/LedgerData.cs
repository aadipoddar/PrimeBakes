using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class LedgerData
{
    public static async Task<int> InsertLedger(LedgerModel ledger, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertLedger, ledger, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<LedgerModel> LoadLedgerByLocation(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<LedgerModel, dynamic>(StoredProcedureNames.LoadLedgerByLocation, new { LocationId }, sqlDataAccessTransaction)).FirstOrDefault();
}
