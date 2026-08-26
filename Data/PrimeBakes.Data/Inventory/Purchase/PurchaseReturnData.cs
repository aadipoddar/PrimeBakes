using Dapper;

using System.Data;

using PrimeBakes.Data.Accounts.FinancialAccounting;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Inventory.Purchase;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.Purchase;

public static class PurchaseReturnData
{

	private static async Task<int> InsertPurchaseReturn(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseReturn, purchaseReturn, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Return.");

	private static async Task<int> InsertPurchaseReturnDetail(PurchaseReturnDetailModel purchaseReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseReturnDetail, purchaseReturnDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Return Detail.");

	private static async Task InsertPurchaseReturnDetailList(DataTable purchaseReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseReturnDetailList, new { PurchaseReturnDetails = purchaseReturnDetails.AsTableValuedParameter(InventoryNames.PurchaseReturnDetailType) }, sqlDataAccessTransaction);

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var purchaseReturns = await CommonData.LoadTableDataByFinancialAccountingId<PurchaseReturnModel>(InventoryNames.PurchaseReturn, financialAccountingId, sqlDataAccessTransaction);
		foreach (var purchaseReturn in purchaseReturns)
		{
			purchaseReturn.FinancialAccountingId = newFinancialAccountingId;
			await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
		}
	}

	public static async Task<PurchaseReturnInvoiceBundle> LoadInvoiceBundle(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, transaction.Id);
		transactionDetails = [.. transactionDetails.OrderBy(detail => detail.ItemName)];
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var party = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, transaction.PartyId);
		if (company is null || party is null)
			throw new InvalidOperationException("Company or party information is missing.");

		return new(transaction, transactionDetails, company, party, await CommonData.LoadCurrentDateTime());
	}

	#region Delete
	public static async Task DeleteTransaction(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(purchaseReturn, transaction));
			await PurchaseReturnNotify.Notify(purchaseReturn.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(purchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

		purchaseReturn.Status = false;
		await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);

		await DeleteAccounting(purchaseReturn, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchaseReturn.TransactionNo, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.PurchaseReturn,
			RecordNo = purchaseReturn.TransactionNo,
			CreatedBy = purchaseReturn.LastModifiedBy.Value,
			CreatedFromPlatform = purchaseReturn.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (purchaseReturn.FinancialAccountingId is null || purchaseReturn.FinancialAccountingId <= 0)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, purchaseReturn.FinancialAccountingId.Value, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = purchaseReturn.LastModifiedBy;
		existingAccounting.LastModifiedAt = purchaseReturn.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = purchaseReturn.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(PurchaseReturnModel purchaseReturn)
	{
		purchaseReturn.Status = true;
		var purchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(InventoryNames.PurchaseReturnDetail, purchaseReturn.Id);
		await SaveTransaction(purchaseReturn, purchaseReturnDetails, true);

		await PurchaseReturnNotify.Notify(purchaseReturn.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<PurchaseReturnModel> ValidateTransaction(PurchaseReturnModel purchaseReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		purchaseReturn.ChallanNo = string.IsNullOrWhiteSpace(purchaseReturn.ChallanNo) ? null : purchaseReturn.ChallanNo.Trim();
		purchaseReturn.Remarks = string.IsNullOrWhiteSpace(purchaseReturn.Remarks) ? null : purchaseReturn.Remarks.Trim();
		purchaseReturn.DocumentUrl = string.IsNullOrWhiteSpace(purchaseReturn.DocumentUrl) ? null : purchaseReturn.DocumentUrl.Trim();

		if (purchaseReturn.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (purchaseReturn.PartyId <= 0)
			throw new InvalidOperationException("Please select a party for the transaction.");

		if (purchaseReturn.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (purchaseReturn.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (purchaseReturn.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (!update)
			purchaseReturn.TransactionNo = await GenerateCodes.GeneratePurchaseReturnTransactionNo(purchaseReturn, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(purchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(InventoryNames.PurchaseReturn, purchaseReturn.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPurchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, purchaseReturn.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			purchaseReturn.TransactionNo = existingPurchaseReturn.TransactionNo;
		}

		return purchaseReturn;
	}

	private static void ValidateItemDetails(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails)
	{
		if (purchaseReturnDetails is null || purchaseReturnDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (purchaseReturnDetails.Count != purchaseReturn.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (purchaseReturnDetails.Any(ed => ed.Total <= 0))
			throw new InvalidOperationException("Item amount must be greater than zero.");

		if (purchaseReturnDetails.Sum(ed => ed.Quantity) != purchaseReturn.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in purchaseReturnDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		PurchaseReturnModel purchaseReturn,
		List<PurchaseReturnDetailModel> purchaseReturnDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = purchaseReturn.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? PurchaseReturnInvoiceExport.ExportInvoice(await LoadInvoiceBundle(purchaseReturn.Id), InvoiceExportType.PDF) : null;

			purchaseReturn.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(purchaseReturn, purchaseReturnDetails, recover, transaction));

			if (!recover)
				await PurchaseReturnNotify.Notify(purchaseReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return purchaseReturn.Id;
		}

		purchaseReturn = await ValidateTransaction(purchaseReturn, update, sqlDataAccessTransaction);
		ValidateItemDetails(purchaseReturn, purchaseReturnDetails);

		var previousPurchaseReturn = update && !recover ? await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturn.Id, sqlDataAccessTransaction) : new();
		var previousPurchaseReturnDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, purchaseReturn.Id, sqlDataAccessTransaction) : [];

		purchaseReturn.Id = await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
		if (!recover) await SaveTransactionDetail(purchaseReturn, purchaseReturnDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(purchaseReturn, purchaseReturnDetails, sqlDataAccessTransaction);
		await SaveAccounting(purchaseReturn, sqlDataAccessTransaction);
		await SaveAuditTrail(purchaseReturn, update, recover, previousPurchaseReturn, previousPurchaseReturnDetails, sqlDataAccessTransaction);

		return purchaseReturn.Id;
	}

	private static async Task SaveTransactionDetail(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		List<PurchaseReturnDetailModel> details = [];

		if (update)
		{
			var existingPurchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(InventoryNames.PurchaseReturnDetail, purchaseReturn.Id, sqlDataAccessTransaction);
			foreach (var item in existingPurchaseReturnDetails)
			{
				item.Status = false;
				details.Add(item);
			}
		}

		foreach (var item in purchaseReturnDetails)
		{
			item.MasterId = purchaseReturn.Id;
			details.Add(item);
		}

		await InsertPurchaseReturnDetailList(SqlDataAccess.ToDataTable(details), sqlDataAccessTransaction);
	}

	private static async Task SaveRawMaterialStock(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchaseReturn.TransactionNo, sqlDataAccessTransaction);

		List<RawMaterialStockModel> stocks = [];

		foreach (var item in purchaseReturnDetails)
			stocks.Add(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				Type = nameof(StockType.PurchaseReturn),
				TransactionId = purchaseReturn.Id,
				TransactionNo = purchaseReturn.TransactionNo,
				TransactionDateTime = purchaseReturn.TransactionDateTime
			});

		await RawMaterialStockData.InsertRawMaterialStockList(SqlDataAccess.ToDataTable(stocks), sqlDataAccessTransaction);
	}

	private static async Task SaveAccounting(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await DeleteAccounting(purchaseReturn, sqlDataAccessTransaction);

		var purchaseReturnOverview = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturn.Id, sqlDataAccessTransaction);
		if (purchaseReturnOverview is null || purchaseReturnOverview.TotalAmount == 0)
			return;

		var purchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, purchaseReturn.Id, sqlDataAccessTransaction);
		if (purchaseReturnDetails is null)
			return;

		var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (purchaseReturnOverview.TotalAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.PurchaseReturn),
				ReferenceNo = purchaseReturnOverview.TransactionNo,
				LedgerId = purchaseReturnOverview.PartyId,
				Debit = purchaseReturnOverview.TotalAmount,
				Credit = null,
				Remarks = $"Party Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
			});

		if (purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.PurchaseReturn),
				ReferenceNo = purchaseReturnOverview.TransactionNo,
				LedgerId = int.Parse(purchaseLedger.Value),
				Debit = null,
				Credit = purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount,
				Remarks = $"Purchase Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
			});

		if (purchaseReturnOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.PurchaseReturn),
				ReferenceNo = purchaseReturnOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = purchaseReturnOverview.TotalExtraTaxAmount,
				Remarks = $"GST Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = purchaseReturnOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = purchaseReturnOverview.Id,
			ReferenceNo = purchaseReturnOverview.TransactionNo,
			TransactionDateTime = purchaseReturnOverview.TransactionDateTime,
			FinancialYearId = purchaseReturnOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = purchaseReturnOverview.Remarks,
			CreatedBy = purchaseReturnOverview.CreatedBy,
			CreatedAt = purchaseReturnOverview.CreatedAt,
			CreatedFromPlatform = purchaseReturnOverview.CreatedFromPlatform,
			Status = true
		};

		var ledgers = accountingCart.ConvertCartToDetails(accounting.Id);
		accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		purchaseReturn.FinancialAccountingId = accounting.Id;
		await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		PurchaseReturnModel purchaseReturn,
		bool update,
		bool recover,
		PurchaseReturnOverviewModel previousPurchaseReturn = null,
		List<PurchaseReturnItemOverviewModel> previousPurchaseReturnDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturn.Id, sqlDataAccessTransaction);
			var currentPurchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, purchaseReturn.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousPurchaseReturn, currentPurchaseReturn);
			var detailsDiff = AuditTrailData.GetDifference(previousPurchaseReturnDetails, currentPurchaseReturnDetails, typeof(PurchaseReturnOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.PurchaseReturn,
			RecordNo = purchaseReturn.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? purchaseReturn.LastModifiedBy.Value : purchaseReturn.CreatedBy,
			CreatedFromPlatform = update ? purchaseReturn.LastModifiedFromPlatform : purchaseReturn.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
