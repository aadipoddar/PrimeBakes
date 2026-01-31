using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class FinancialAccountingData
{
    private static async Task<int> InsertFinancialAccounting(FinancialAccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertFinancialAccounting, accounting, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertFinancialAccountingDetail(FinancialAccountingDetailModel accountingDetails, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertFinancialAccountingDetail, accountingDetails, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<FinancialAccountingModel> LoadFinancialAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<FinancialAccountingModel, dynamic>(StoredProcedureNames.LoadFinancialAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByCompanyDate(int CompanyId, DateTime StartDate, DateTime EndDate) =>
        await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByCompanyDate, new { CompanyId, StartDate, EndDate });

    public static List<FinancialAccountingDetailModel> ConvertCartToDetails(List<FinancialAccountingItemCartModel> cart, int accountingId) =>
        [.. cart.Select(item => new FinancialAccountingDetailModel
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

    public static async Task DeleteTransaction(FinancialAccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sqlDataAccessTransaction is null)
        {
            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                await DeleteTransaction(accounting, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            await FinancialAccountingNotify.Notify(accounting.Id, NotifyType.Deleted);
        }

        try
        {
            await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

            accounting.Status = false;
            await InsertFinancialAccounting(accounting, sqlDataAccessTransaction);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(FinancialAccountingModel accounting)
    {
        accounting.Status = true;
        var accountingDetails = await CommonData.LoadTableDataByMasterId<FinancialAccountingDetailModel>(TableNames.FinancialAccountingDetail, accounting.Id);

        await SaveTransaction(accounting, null, accountingDetails, false);

        await FinancialAccountingNotify.Notify(accounting.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(FinancialAccountingModel accounting, List<FinancialAccountingItemCartModel> cart, List<FinancialAccountingDetailModel> accountingDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = accounting.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await FinancialAccountingInvoiceExport.ExportInvoice(accounting.Id, InvoiceExportType.PDF);

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
                await FinancialAccountingNotify.Notify(accounting.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return accounting.Id;
        }

        if (update)
        {
            var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, accounting.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingAccounting.TransactionDateTime, sqlDataAccessTransaction);
            accounting.TransactionNo = existingAccounting.TransactionNo;
        }
        else
            accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

        accounting.Id = await InsertFinancialAccounting(accounting, sqlDataAccessTransaction);
        accountingDetails ??= ConvertCartToDetails(cart, accounting.Id);
        await SaveTransactionDetail(accounting, accountingDetails, update, sqlDataAccessTransaction);

        return accounting.Id;
    }

    private static async Task SaveTransactionDetail(FinancialAccountingModel accounting, List<FinancialAccountingDetailModel> accountingDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (accountingDetails is null || accountingDetails.Count != (accounting.TotalDebitLedgers + accounting.TotalCreditLedgers))
            throw new InvalidOperationException("Accounting details do not match the transaction summary.");

        if (accountingDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Accounting detail items must be active.");

        if (update)
        {
            var existingAccountingDetails = await CommonData.LoadTableDataByMasterId<FinancialAccountingDetailModel>(TableNames.FinancialAccountingDetail, accounting.Id, sqlDataAccessTransaction);
            foreach (var item in existingAccountingDetails)
            {
                item.Status = false;
                await InsertFinancialAccountingDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in accountingDetails)
        {
            item.MasterId = accounting.Id;
            var id = await InsertFinancialAccountingDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save accounting detail item.");
        }
    }
}
