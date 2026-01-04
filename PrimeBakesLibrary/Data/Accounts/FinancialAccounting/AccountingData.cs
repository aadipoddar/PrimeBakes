using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class AccountingData
{
    private static async Task<int> InsertAccounting(AccountingModel accounting) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccounting, accounting)).FirstOrDefault();

    private static async Task<int> InsertAccountingDetail(AccountingDetailModel accountingDetails) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccountingDetail, accountingDetails)).FirstOrDefault();

    public static async Task<AccountingModel> LoadAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo) =>
        (await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo })).FirstOrDefault();

    public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByCompanyDate(int CompanyId, DateTime StartDate, DateTime EndDate) =>
        await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByCompanyDate, new { CompanyId, StartDate, EndDate });

    public static async Task DeleteTransaction(AccountingModel accounting)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        accounting.Status = false;
        await InsertAccounting(accounting);
        await SendNotification.FinancialAccountingNotification(accounting.Id, NotificationType.Delete);
    }

    public static async Task RecoverTransaction(AccountingModel accounting)
    {
        var accountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id);
        List<AccountingItemCartModel> accountingItemCarts = [];

        foreach (var item in accountingDetails)
            accountingItemCarts.Add(new()
            {
                LedgerName = string.Empty,
                LedgerId = item.LedgerId,
                ReferenceType = item.ReferenceType,
                Credit = item.Credit,
                Debit = item.Debit,
                ReferenceId = item.ReferenceId,
                ReferenceNo = item.ReferenceId.HasValue ? item.ReferenceId.Value.ToString() : null,
                Remarks = item.Remarks
            });

        await SaveTransaction(accounting, accountingItemCarts, false);
        await SendNotification.FinancialAccountingNotification(accounting.Id, NotificationType.Recover);
    }

    public static async Task<int> SaveTransaction(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails, bool showNotification = true)
    {
        bool update = accounting.Id > 0;

        if (update)
        {
            var existingAccounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accounting.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingAccounting.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            accounting.TransactionNo = existingAccounting.TransactionNo;
        }
        else
            accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        accounting.Id = await InsertAccounting(accounting);
        await SaveTransactionDetail(accounting, accountingDetails, update);

        if (showNotification && update)
            await SendNotification.FinancialAccountingNotification(accounting.Id, update ? NotificationType.Update : NotificationType.Save);

        return accounting.Id;
    }

    private static async Task SaveTransactionDetail(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails, bool update)
    {
        if (update)
        {
            var existingAccountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id);
            foreach (var item in existingAccountingDetails)
            {
                item.Status = false;
                await InsertAccountingDetail(item);
            }
        }

        foreach (var item in accountingDetails)
            await InsertAccountingDetail(new()
            {
                Id = 0,
                MasterId = accounting.Id,
                LedgerId = item.LedgerId,
                Credit = item.Credit,
                Debit = item.Debit,
                ReferenceType = item.ReferenceType,
                ReferenceId = item.ReferenceId,
                ReferenceNo = item.ReferenceNo,
                Remarks = item.Remarks,
                Status = true
            });
    }
}
