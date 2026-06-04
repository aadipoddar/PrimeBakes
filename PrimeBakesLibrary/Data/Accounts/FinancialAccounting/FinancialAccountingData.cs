using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.Data.Store.Sale;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Operations;

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

			var billDayClosingVoucherId = await SettingsData.LoadSettingsByKey(SettingsKeys.BillDayCloseVoucherId, sqlDataAccessTransaction);
			if (accounting.VoucherId == int.Parse(billDayClosingVoucherId.Value))
				await BillData.UpdateBillsFinancialAccountingId(accounting.Id, sqlDataAccessTransaction);

			var saleDayClosingVoucherId = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleDayCloseVoucherId, sqlDataAccessTransaction);
			if (accounting.VoucherId == int.Parse(saleDayClosingVoucherId.Value))
				await SaleData.UpdateSalesFinancialAccountingId(accounting.Id, sqlDataAccessTransaction);
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

	private static async Task<FinancialAccountingModel> ValidateTransaction(FinancialAccountingModel accounting, bool update = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (accounting.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (accounting.VoucherId <= 0)
			throw new InvalidOperationException("Please select a voucher for the transaction.");

		if (accounting.TotalCreditAmount <= 0 && accounting.TotalDebitAmount <= 0)
			throw new InvalidOperationException("Total debit and credit amounts cannot both be zero.");

		if (accounting.TotalCreditLedgers <= 0 && accounting.TotalDebitLedgers <= 0)
			throw new InvalidOperationException("There must be at least one debit or credit ledger in the transaction.");

		if (accounting.TotalDebitAmount - accounting.TotalCreditAmount != 0)
			throw new InvalidOperationException("Total debit and credit amounts must be equal.");

		accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, accounting.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingAccounting.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, accounting.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			accounting.TransactionNo = existingAccounting.TransactionNo;
		}

		return accounting;
	}

	private static async Task<List<FinancialAccountingDetailModel>> ValidateTransactionDetails(FinancialAccountingModel accounting, List<FinancialAccountingItemCartModel> cart, List<FinancialAccountingDetailModel> accountingDetails = null, bool update = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		accountingDetails ??= ConvertCartToDetails(cart, accounting.Id);

		if (accountingDetails is null || accountingDetails.Count == 0)
			throw new InvalidOperationException("The transaction must have at least one accounting detail item.");

		if (accountingDetails.Any(d => !d.Status))
			throw new InvalidOperationException("Accounting detail items must be active.");

		if (accountingDetails.Any(d => (d.Credit ?? 0) < 0 || (d.Debit ?? 0) < 0))
			throw new InvalidOperationException("Accounting detail items cannot have negative amounts.");

		if (accountingDetails.Count != (accounting.TotalDebitLedgers + accounting.TotalCreditLedgers))
			throw new InvalidOperationException("The number of accounting detail items does not match the transaction summary.");

		return accountingDetails;
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

		accounting = await ValidateTransaction(accounting, update, sqlDataAccessTransaction);
		accounting.Id = await InsertFinancialAccounting(accounting, sqlDataAccessTransaction);
		accountingDetails = await ValidateTransactionDetails(accounting, cart, accountingDetails, update, sqlDataAccessTransaction);
		await SaveTransactionDetail(accounting, accountingDetails, update, sqlDataAccessTransaction);

		return accounting.Id;
	}

	private static async Task SaveTransactionDetail(FinancialAccountingModel accounting, List<FinancialAccountingDetailModel> accountingDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
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
