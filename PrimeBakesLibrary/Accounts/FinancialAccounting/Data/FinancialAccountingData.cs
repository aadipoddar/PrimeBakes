using PrimeBakesLibrary.Accounts.FinancialAccounting.Exports;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Purchase.Data;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Accounts.FinancialAccounting.Data;

public static class FinancialAccountingData
{
	private static async Task<int> InsertFinancialAccounting(FinancialAccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertFinancialAccounting, accounting, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Financial Accounting.");

	private static async Task<int> InsertFinancialAccountingLedger(FinancialAccountingLedgerModel ledger, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertFinancialAccountingLedger, ledger, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Financial Accounting Ledger.");

	public static async Task<FinancialAccountingModel> LoadFinancialAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<FinancialAccountingModel, dynamic>(AccountNames.LoadFinancialAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByCompanyDate(int CompanyId, DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(AccountNames.LoadTrialBalanceByCompanyDate, new { CompanyId, StartDate, EndDate });

	public static List<FinancialAccountingLedgerModel> ConvertCartToDetails(List<FinancialAccountingLedgerCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new FinancialAccountingLedgerModel
		{
			Id = 0,
			MasterId = masterId,
			LedgerId = item.LedgerId,
			Credit = item.Credit,
			Debit = item.Debit,
			ReferenceType = item.ReferenceType,
			ReferenceId = item.ReferenceId,
			ReferenceNo = item.ReferenceNo,
			InstrumentNo = item.InstrumentNo,
			InstrumentDate = item.InstrumentDate,
			Remarks = item.Remarks,
			Status = true
		})];

	public static async Task DeleteTransaction(FinancialAccountingModel accounting, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(accounting, transaction));
			await FinancialAccountingNotify.Notify(accounting.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);
		await ValidateBRS(accounting.Id, sqlDataAccessTransaction);
		await DeletePostings(accounting.Id, sqlDataAccessTransaction);

		accounting.Status = false;
		await InsertFinancialAccounting(accounting, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = AccountNames.FinancialAccounting,
			RecordNo = accounting.TransactionNo,
			CreatedBy = accounting.LastModifiedBy.Value,
			CreatedFromPlatform = accounting.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeletePostings(int id, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await PurchaseData.UpdateFinancialAccountingId(id, null, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(FinancialAccountingModel accounting)
	{
		if (accounting.ReferenceId is not null || !string.IsNullOrWhiteSpace(accounting.ReferenceNo))
			throw new InvalidOperationException("Transactions with reference cannot be recovered. Please create a new transaction instead.");

		accounting.Status = true;
		var ledgers = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerModel>(AccountNames.FinancialAccountingLedger, accounting.Id);

		await SaveTransaction(accounting, ledgers, recover: true);

		await FinancialAccountingNotify.Notify(accounting.Id, NotifyType.Recovered);
	}

	#region Saving
	private static async Task<FinancialAccountingModel> ValidateTransaction(FinancialAccountingModel accounting, bool update = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		accounting.Remarks = string.IsNullOrWhiteSpace(accounting.Remarks) ? null : accounting.Remarks.Trim();

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

		if (!update)
			accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(accounting.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, accounting.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingAccounting.TransactionDateTime, sqlDataAccessTransaction);

			await ValidateBRS(accounting.Id, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, accounting.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			accounting.TransactionNo = existingAccounting.TransactionNo;
		}

		return accounting;
	}

	private static void ValidateTransactionLedgers(FinancialAccountingModel accounting, List<FinancialAccountingLedgerModel> ledgers)
	{
		if (ledgers is null || ledgers.Count == 0)
			throw new InvalidOperationException("The transaction must have at least one accounting ledger.");

		if (ledgers.Any(d => !d.Status))
			throw new InvalidOperationException("Accounting ledgers must be active.");

		if (ledgers.Any(d => (d.Credit ?? 0) < 0 || (d.Debit ?? 0) < 0))
			throw new InvalidOperationException("Accounting ledgers cannot have negative amounts.");

		if (ledgers.Count != (accounting.TotalDebitLedgers + accounting.TotalCreditLedgers))
			throw new InvalidOperationException("The number of accounting ledgers does not match the transaction summary.");

		foreach (var item in ledgers)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		FinancialAccountingModel accounting,
		List<FinancialAccountingLedgerModel> ledgers,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = accounting.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await FinancialAccountingInvoiceExport.ExportInvoice(accounting.Id, InvoiceExportType.PDF) : null;

			accounting.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(accounting, ledgers, recover, transaction));

			if (!recover)
				await FinancialAccountingNotify.Notify(accounting.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return accounting.Id;
		}

		accounting = await ValidateTransaction(accounting, update, sqlDataAccessTransaction);
		ValidateTransactionLedgers(accounting, ledgers);

		var previousAccounting = update && !recover ? await CommonData.LoadTableDataById<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, accounting.Id, sqlDataAccessTransaction) : null;
		var previousLedgers = update && !recover ? await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerOverviewModel>(AccountNames.FinancialAccountingLedgerOverview, accounting.Id, sqlDataAccessTransaction) : null;

		accounting.Id = await InsertFinancialAccounting(accounting, sqlDataAccessTransaction);
		await SaveTransactionLedgerDetails(accounting, ledgers, update, sqlDataAccessTransaction);
		await SaveAuditTrail(accounting, update, recover, previousAccounting, previousLedgers, sqlDataAccessTransaction);

		return accounting.Id;
	}

	private static async Task SaveTransactionLedgerDetails(FinancialAccountingModel accounting, List<FinancialAccountingLedgerModel> ledgers, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingLedgers = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerModel>(AccountNames.FinancialAccountingLedger, accounting.Id, sqlDataAccessTransaction);
			foreach (var item in existingLedgers)
			{
				item.Status = false;
				await InsertFinancialAccountingLedger(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in ledgers)
		{
			item.MasterId = accounting.Id;
			await InsertFinancialAccountingLedger(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAuditTrail(
		FinancialAccountingModel accounting,
		bool update,
		bool recover,
		FinancialAccountingOverviewModel previousAccounting = null,
		List<FinancialAccountingLedgerOverviewModel> previousLedgers = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentAccounting = await CommonData.LoadTableDataById<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, accounting.Id, sqlDataAccessTransaction);
			var currentLedgers = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerOverviewModel>(AccountNames.FinancialAccountingLedgerOverview, accounting.Id, sqlDataAccessTransaction);

			var accoutingDiff = AuditTrailData.GetDifference(previousAccounting, currentAccounting);
			var ledgerDiff = AuditTrailData.GetDifference(previousLedgers, currentLedgers, typeof(FinancialAccountingOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, accoutingDiff),
				("Details", ledgerDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = AccountNames.FinancialAccounting,
			RecordNo = accounting.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? accounting.LastModifiedBy.Value : accounting.CreatedBy,
			CreatedFromPlatform = update ? accounting.LastModifiedFromPlatform : accounting.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	#region BRS
	private static async Task ValidateBRS(int masterId, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		var existingLedgers = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerModel>(AccountNames.FinancialAccountingLedger, masterId, sqlDataAccessTransaction);
		if (existingLedgers.Any(l => l.Status && l.ClearingDate.HasValue))
			throw new InvalidOperationException("This transaction cannot be modified or deleted because one or more of its lines have been bank reconciled. Remove the clearing date in Bank Reconciliation first.");
	}

	public static async Task SaveBRSDates(List<FinancialAccountingLedgerModel> changedLines, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var line in changedLines)
			{
				var existingLine = await CommonData.LoadTableDataById<FinancialAccountingLedgerModel>(AccountNames.FinancialAccountingLedger, line.Id, transaction)
					?? throw new InvalidOperationException("The ledger entry to be updated does not exist.");

				var accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, existingLine.MasterId, transaction)
					?? throw new InvalidOperationException("The financial accounting transaction for the ledger entry does not exist.");

				existingLine.ClearingDate = line.ClearingDate;
				if (existingLine.ClearingDate.HasValue && existingLine.ClearingDate.Value.Date < (existingLine.InstrumentDate ?? accounting.TransactionDateTime).Date)
					throw new InvalidOperationException("The clearing date cannot be earlier than the transaction or instrument date.");

				await InsertFinancialAccountingLedger(existingLine, transaction);

				await AuditTrailData.SaveAuditTrail(new()
				{
					Action = AuditTrailActionTypes.Update.ToString(),
					TableName = AccountNames.FinancialAccountingLedger,
					RecordNo = accounting.TransactionNo,
					RecordValue = existingLine.ClearingDate.HasValue ? $"Clearing Date: {existingLine.ClearingDate:yyyy-MM-dd}" : "Clearing Date Removed",
					CreatedBy = userId,
					CreatedFromPlatform = platform
				}, transaction);
			}
		});
	#endregion
}
