using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class FinancialYearData
{
    public static async Task<int> InsertFinancialYear(FinancialYearModel financialYear) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertFinancialYear, financialYear)).FirstOrDefault();

    public static async Task<FinancialYearModel> LoadFinancialYearByDateTime(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<FinancialYearModel, dynamic>(StoredProcedureNames.LoadFinancialYearByDateTime, new { TransactionDateTime }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task ValidateFinancialYear(DateTime TransactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var financialYear = await LoadFinancialYearByDateTime(TransactionDateTime, sqlDataAccessTransaction) ??
            throw new InvalidOperationException("No financial year found for the given date.");

        if (financialYear.Locked)
            throw new InvalidOperationException("The financial year for the given date is locked");

        if (!financialYear.Status)
            throw new InvalidOperationException("The financial year for the given date is inactive.");
    }
}