using Dapper;

using PrimeBakes.Data.Accounts.FinancialAccounting;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Inventory.PurchaseOrder;
using PrimeBakes.Data.Inventory.RawMaterial;
using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Inventory.Purchase;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;

using System.Data;

namespace PrimeBakes.Data.Inventory.Purchase;

public static class PurchaseData
{
	private static async Task<int> InsertPurchase(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchase, purchase, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase.");

	private static async Task<int> InsertPurchaseDetail(PurchaseDetailModel purchaseDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseDetail, purchaseDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Detail.");

	private static async Task InsertPurchaseDetailList(DataTable purchaseDetails, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseDetailList, new { PurchaseDetails = purchaseDetails.AsTableValuedParameter(InventoryNames.PurchaseDetailType) }, sqlDataAccessTransaction);

	public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(InventoryNames.LoadRawMaterialByPartyPurchaseDateTime, new { PartyId, PurchaseDateTime, OnlyActive });

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var purchases = await CommonData.LoadTableDataByFinancialAccountingId<PurchaseModel>(InventoryNames.Purchase, financialAccountingId, sqlDataAccessTransaction);
		foreach (var purchase in purchases)
		{
			purchase.FinancialAccountingId = newFinancialAccountingId;
			await InsertPurchase(purchase, sqlDataAccessTransaction);
		}
	}

	public static async Task<PurchaseInvoiceBundle> LoadInvoiceBundle(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, transaction.Id);
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
	public static async Task DeleteTransaction(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(purchase, transaction));
			await PurchaseNotify.Notify(purchase.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(purchase.TransactionDateTime, sqlDataAccessTransaction);

		if (purchase.PurchaseOrderId is not null && purchase.PurchaseOrderId > 0)
			await PurchaseOrderData.LinkPurchaseOrderToPurchase(purchase.PurchaseOrderId, purchase.Id, true, sqlDataAccessTransaction);

		purchase.PurchaseOrderId = null;
		purchase.Status = false;
		await InsertPurchase(purchase, sqlDataAccessTransaction);

		await DeleteAccounting(purchase, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.TransactionNo, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.Purchase,
			RecordNo = purchase.TransactionNo,
			CreatedBy = purchase.LastModifiedBy.Value,
			CreatedFromPlatform = purchase.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (purchase.FinancialAccountingId is null || purchase.FinancialAccountingId <= 0)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, purchase.FinancialAccountingId.Value, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
		existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(PurchaseModel purchase)
	{
		purchase.Status = true;
		var purchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(InventoryNames.PurchaseDetail, purchase.Id);
		await SaveTransaction(purchase, purchaseDetails, true);

		await PurchaseNotify.Notify(purchase.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<PurchaseModel> ValidateTransaction(PurchaseModel purchase, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		purchase.ChallanNo = string.IsNullOrWhiteSpace(purchase.ChallanNo) ? null : purchase.ChallanNo.Trim();
		purchase.Remarks = string.IsNullOrWhiteSpace(purchase.Remarks) ? null : purchase.Remarks.Trim();
		purchase.DocumentUrl = string.IsNullOrWhiteSpace(purchase.DocumentUrl) ? null : purchase.DocumentUrl.Trim();

		if (purchase.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (purchase.PartyId <= 0)
			throw new InvalidOperationException("Please select a party for the transaction.");

		if (purchase.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (purchase.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (purchase.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (purchase.PurchaseOrderId is not null && purchase.PurchaseOrderId > 0)
		{
			var purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, purchase.PurchaseOrderId.Value, sqlDataAccessTransaction);
			if (purchaseOrder is null || !purchaseOrder.Status)
				throw new InvalidOperationException("The selected purchase order is invalid or does not exist.");

			if (purchaseOrder.PartyId != purchase.PartyId)
				throw new InvalidOperationException("The selected purchase order does not belong to the selected party.");

			if (purchaseOrder.PurchaseId is not null && purchaseOrder.PurchaseId != purchase.Id)
				throw new InvalidOperationException("The selected purchase order is already linked to another purchase.");
		}

		if (!update)
			purchase.TransactionNo = await GenerateCodes.GeneratePurchaseTransactionNo(purchase, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(purchase.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingPurchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, purchase.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPurchase.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, purchase.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			purchase.TransactionNo = existingPurchase.TransactionNo;
		}

		return purchase;
	}

	private static void ValidateItemDetails(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails)
	{
		if (purchaseDetails is null || purchaseDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (purchaseDetails.Count != purchase.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (purchaseDetails.Any(ed => ed.Total <= 0))
			throw new InvalidOperationException("Item amount must be greater than zero.");

		if (purchaseDetails.Sum(ed => ed.Quantity) != purchase.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in purchaseDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		PurchaseModel purchase,
		List<PurchaseDetailModel> purchaseDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = purchase.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? PurchaseInvoiceExport.ExportInvoice(await LoadInvoiceBundle(purchase.Id), InvoiceExportType.PDF) : null;

			purchase.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(purchase, purchaseDetails, recover, transaction));

			if (!recover)
				await PurchaseNotify.Notify(purchase.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return purchase.Id;
		}

		purchase = await ValidateTransaction(purchase, update, sqlDataAccessTransaction);
		ValidateItemDetails(purchase, purchaseDetails);

		var previousPurchase = update && !recover ? await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchase.Id, sqlDataAccessTransaction) : new();
		var previousPurchaseDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, purchase.Id, sqlDataAccessTransaction) : [];

		purchase.Id = await InsertPurchase(purchase, sqlDataAccessTransaction);
		if (!recover) await SaveTransactionDetail(purchase, purchaseDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(purchase, purchaseDetails, sqlDataAccessTransaction);
		await UpdatePurchaseOrder(purchase, previousPurchase, update, sqlDataAccessTransaction);
		await SaveAccounting(purchase, sqlDataAccessTransaction);
		await UpdateRawMaterialRateAndUOMOnPurchase(purchaseDetails, sqlDataAccessTransaction);
		await SaveAuditTrail(purchase, update, recover, previousPurchase, previousPurchaseDetails, sqlDataAccessTransaction);

		return purchase.Id;
	}

	private static async Task SaveTransactionDetail(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		List<PurchaseDetailModel> details = [];

		if (update)
		{
			var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(InventoryNames.PurchaseDetail, purchase.Id, sqlDataAccessTransaction);
			foreach (var item in existingPurchaseDetails)
			{
				item.Status = false;
				details.Add(item);
			}
		}

		foreach (var item in purchaseDetails)
		{
			item.MasterId = purchase.Id;
			details.Add(item);
		}

		await InsertPurchaseDetailList(SqlDataAccess.ToDataTable(details), sqlDataAccessTransaction);
	}

	private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.TransactionNo, sqlDataAccessTransaction);

		List<RawMaterialStockModel> stocks = [];

		foreach (var item in purchaseDetails)
			stocks.Add(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				Type = nameof(StockType.Purchase),
				TransactionId = purchase.Id,
				TransactionNo = purchase.TransactionNo,
				TransactionDateTime = purchase.TransactionDateTime
			});

		await RawMaterialStockData.InsertRawMaterialStockList(SqlDataAccess.ToDataTable(stocks), sqlDataAccessTransaction);
	}

	private static async Task UpdatePurchaseOrder(PurchaseModel purchase, PurchaseOverviewModel previousPurchase, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await PurchaseOrderData.LinkPurchaseOrderToPurchase(previousPurchase.PurchaseOrderId, previousPurchase.Id, true, sqlDataAccessTransaction);

		if (purchase.PurchaseOrderId is not null)
			await PurchaseOrderData.LinkPurchaseOrderToPurchase(purchase.PurchaseOrderId, purchase.Id, false, sqlDataAccessTransaction);
	}

	private static async Task SaveAccounting(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await DeleteAccounting(purchase, sqlDataAccessTransaction);

		var purchaseOverview = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchase.Id, sqlDataAccessTransaction);
		if (purchaseOverview is null || purchaseOverview.TotalAmount == 0)
			return;

		var purchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, purchase.Id, sqlDataAccessTransaction);
		if (purchaseDetails is null)
			return;

		var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (purchaseOverview.TotalAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Purchase),
				ReferenceNo = purchaseOverview.TransactionNo,
				LedgerId = purchaseOverview.PartyId,
				Debit = null,
				Credit = purchaseOverview.TotalAmount,
				Remarks = $"Party Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
			});

		if (purchaseOverview.TotalAmount - purchaseOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Purchase),
				ReferenceNo = purchaseOverview.TransactionNo,
				LedgerId = int.Parse(purchaseLedger.Value),
				Debit = purchaseOverview.TotalAmount - purchaseOverview.TotalExtraTaxAmount,
				Credit = null,
				Remarks = $"Purchase Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
			});

		if (purchaseOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = purchaseOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Purchase),
				ReferenceNo = purchaseOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = purchaseOverview.TotalExtraTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = purchaseOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = purchaseOverview.Id,
			ReferenceNo = purchaseOverview.TransactionNo,
			TransactionDateTime = purchaseOverview.TransactionDateTime,
			FinancialYearId = purchaseOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = purchaseOverview.Remarks,
			CreatedBy = purchaseOverview.CreatedBy,
			CreatedAt = purchaseOverview.CreatedAt,
			CreatedFromPlatform = purchaseOverview.CreatedFromPlatform,
			Status = true
		};

		var ledgers = accountingCart.ConvertCartToDetails(accounting.Id);
		accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		purchase.FinancialAccountingId = accounting.Id;
		await InsertPurchase(purchase, sqlDataAccessTransaction);
	}

	private static async Task UpdateRawMaterialRateAndUOMOnPurchase(List<PurchaseDetailModel> purchaseDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		var isUpdateItemRateOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterRateOnPurchase, sqlDataAccessTransaction)).Value);
		var isUpdateItemUOMOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterUOMOnPurchase, sqlDataAccessTransaction)).Value);

		if (!isUpdateItemRateOnPurchaseEnabled && !isUpdateItemUOMOnPurchaseEnabled)
			return;

		var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(InventoryNames.RawMaterial);

		foreach (var purchaseItem in purchaseDetails)
		{
			var rawMaterial = rawMaterials.FirstOrDefault(i => i.Id == purchaseItem.RawMaterialId);
			if (rawMaterial is not null)
			{
				if (isUpdateItemRateOnPurchaseEnabled)
					rawMaterial.Rate = purchaseItem.Rate;
				if (isUpdateItemUOMOnPurchaseEnabled)
					rawMaterial.UnitOfMeasurement = purchaseItem.UnitOfMeasurement;

				await RawMaterialData.InsertRawMaterial(rawMaterial, sqlDataAccessTransaction);
			}
		}
	}

	private static async Task SaveAuditTrail(
		PurchaseModel purchase,
		bool update,
		bool recover,
		PurchaseOverviewModel previousPurchase = null,
		List<PurchaseItemOverviewModel> previousPurchaseDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentPurchase = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchase.Id, sqlDataAccessTransaction);
			var currentPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, purchase.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousPurchase, currentPurchase);
			var detailsDiff = AuditTrailData.GetDifference(previousPurchaseDetails, currentPurchaseDetails, typeof(PurchaseOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.Purchase,
			RecordNo = purchase.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? purchase.LastModifiedBy.Value : purchase.CreatedBy,
			CreatedFromPlatform = update ? purchase.LastModifiedFromPlatform : purchase.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
