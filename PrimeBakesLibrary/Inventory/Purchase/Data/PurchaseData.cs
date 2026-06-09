using PrimeBakesLibrary.Accounts.FinancialAccounting.Data;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Purchase.Exports;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Inventory.RawMaterial.Data;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Purchase.Data;

public static class PurchaseData
{
	private static async Task<int> InsertPurchase(PurchaseModel purchase, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchase, purchase, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase.");

	private static async Task<int> InsertPurchaseDetail(PurchaseDetailModel purchaseDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseDetail, purchaseDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Detail.");

	public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(InventoryNames.LoadRawMaterialByPartyPurchaseDateTime, new { PartyId, PurchaseDateTime, OnlyActive });

	public static List<PurchaseDetailModel> ConvertCartToDetails(List<PurchaseItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new PurchaseDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			BaseTotal = item.BaseTotal,
			DiscountPercent = item.DiscountPercent,
			DiscountAmount = item.DiscountAmount,
			AfterDiscount = item.AfterDiscount,
			CGSTPercent = item.CGSTPercent,
			CGSTAmount = item.CGSTAmount,
			SGSTPercent = item.SGSTPercent,
			SGSTAmount = item.SGSTAmount,
			IGSTPercent = item.IGSTPercent,
			IGSTAmount = item.IGSTAmount,
			TotalTaxAmount = item.TotalTaxAmount,
			InclusiveTax = item.InclusiveTax,
			NetRate = item.NetRate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var purchases = await CommonData.LoadTableDataByFinancialAccountingId<PurchaseModel>(InventoryNames.Purchase, financialAccountingId, sqlDataAccessTransaction);
		foreach (var purchase in purchases)
		{
			purchase.FinancialAccountingId = newFinancialAccountingId;
			await InsertPurchase(purchase, sqlDataAccessTransaction);
		}
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

		purchase.Status = false;
		await InsertPurchase(purchase, sqlDataAccessTransaction);

		await DeleteAccounting(purchase, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id, sqlDataAccessTransaction);

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
		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, purchase.FinancialAccountingId ?? 0, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
		existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(PurchaseModel purchase)
	{
		purchase.Status = true;
		var purchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(InventoryNames.PurchaseDetail, purchase.Id);
		await SaveTransaction(purchase, purchaseDetails, true);

		await PurchaseNotify.Notify(purchase.Id, NotifyType.Recovered);
	}

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

		if (!update)
			purchase.TransactionNo = await GenerateCodes.GeneratePurchaseTransactionNo(purchase, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(purchase.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingPurchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, purchase.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The purchase transaction does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPurchase.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, purchase.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users can update a purchase transaction.");

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
			(MemoryStream, string)? previousInvoice = update && !recover ? await PurchaseInvoiceExport.ExportInvoice(purchase.Id, InvoiceExportType.PDF) : null;

			purchase.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(purchase, purchaseDetails, recover, transaction));

			if (!recover)
				await PurchaseNotify.Notify(purchase.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return purchase.Id;
		}

		purchase = await ValidateTransaction(purchase, update, sqlDataAccessTransaction);
		ValidateItemDetails(purchase, purchaseDetails);

		var previousPurchase = update && !recover ? await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, purchase.Id, sqlDataAccessTransaction) : null;
		var previousPurchaseDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, purchase.Id, sqlDataAccessTransaction) : null;

		purchase.Id = await InsertPurchase(purchase, sqlDataAccessTransaction);
		await SaveTransactionDetail(purchase, purchaseDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(purchase, purchaseDetails, update, sqlDataAccessTransaction);
		await SaveAccounting(purchase, update, sqlDataAccessTransaction);
		await UpdateRawMaterialRateAndUOMOnPurchase(purchaseDetails, sqlDataAccessTransaction);
		await SaveAuditTrail(purchase, update, recover, previousPurchase, previousPurchaseDetails, sqlDataAccessTransaction);

		return purchase.Id;
	}

	private static async Task SaveTransactionDetail(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(InventoryNames.PurchaseDetail, purchase.Id, sqlDataAccessTransaction);
			foreach (var item in existingPurchaseDetails)
			{
				item.Status = false;
				await InsertPurchaseDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in purchaseDetails)
		{
			item.MasterId = purchase.Id;
			await InsertPurchaseDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseDetailModel> purchaseDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Purchase), purchase.Id, sqlDataAccessTransaction);

		foreach (var item in purchaseDetails)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				Type = nameof(StockType.Purchase),
				TransactionId = purchase.Id,
				TransactionNo = purchase.TransactionNo,
				TransactionDateTime = purchase.TransactionDateTime
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAccounting(PurchaseModel purchase, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId, sqlDataAccessTransaction);
			var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(purchaseVoucher.Value), purchase.Id, purchase.TransactionNo, sqlDataAccessTransaction);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = purchase.LastModifiedBy;
				existingAccounting.LastModifiedAt = purchase.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = purchase.LastModifiedFromPlatform;

				await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
			}
		}

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

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
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