using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class AccountingData
{
    private static async Task<int> InsertAccounting(AccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccounting, accounting, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertAccountingDetail(AccountingDetailModel accountingDetails, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccountingDetail, accountingDetails, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<AccountingModel> LoadAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByCompanyDate(int CompanyId, DateTime StartDate, DateTime EndDate) =>
        await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByCompanyDate, new { CompanyId, StartDate, EndDate });

    public static List<AccountingDetailModel> ConvertCartToDetails(List<AccountingItemCartModel> cart, int accountingId) =>
        [.. cart.Select(item => new AccountingDetailModel
        {
            Id = 0,
            MasterId = accountingId,
            LedgerId = item.LedgerId,
            Credit = item.Credit,
            Debit = item.Debit,
            ReferenceType = item.ReferenceType,
            ReferenceId = item.ReferenceId,
            ReferenceNo = item.ReferenceNo,
            Remarks = item.Remarks,
            Status = true
        })];

    public static async Task DeleteTransaction(AccountingModel accounting)
    {
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

            accounting.Status = false;
            await InsertAccounting(accounting, sqlDataAccessTransaction);

            sqlDataAccessTransaction.CommitTransaction();

            await AccountingNotify.Notify(accounting.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(AccountingModel accounting)
    {
        accounting.Status = true;
        var accountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id);

        await SaveTransaction(accounting, null, accountingDetails, false);

        await AccountingNotify.Notify(accounting.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(AccountingModel accounting, List<AccountingItemCartModel> cart, List<AccountingDetailModel> accountingDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = accounting.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await AccountingInvoicePDFExport.ExportInvoice(accounting.Id);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                accounting.Id = await SaveTransaction(accounting, cart, accountingDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await AccountingNotify.Notify(accounting.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return accounting.Id;
        }

        if (update)
        {
            var existingAccounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accounting.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingAccounting.TransactionDateTime, sqlDataAccessTransaction);
            accounting.TransactionNo = existingAccounting.TransactionNo;
        }
        else
            accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

        accounting.Id = await InsertAccounting(accounting, sqlDataAccessTransaction);
        accountingDetails ??= ConvertCartToDetails(cart, accounting.Id);
        await SaveTransactionDetail(accounting, accountingDetails, update, sqlDataAccessTransaction);

        return accounting.Id;
    }

    private static async Task SaveTransactionDetail(AccountingModel accounting, List<AccountingDetailModel> accountingDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (accountingDetails is null || accountingDetails.Count != (accounting.TotalDebitLedgers + accounting.TotalCreditLedgers))
            throw new InvalidOperationException("Accounting details do not match the transaction summary.");

        if (accountingDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Accounting detail items must be active.");

        if (update)
        {
            var existingAccountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id, sqlDataAccessTransaction);
            foreach (var item in existingAccountingDetails)
            {
                item.Status = false;
                await InsertAccountingDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in accountingDetails)
        {
            item.MasterId = accounting.Id;
            var id = await InsertAccountingDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save accounting detail item.");
        }
    }
}
