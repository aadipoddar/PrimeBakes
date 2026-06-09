using PrimeBakesLibrary.Accounts.FinancialAccounting.Data;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Purchase.Exports;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Purchase.Data;

public static class PurchaseReturnData
{
	private static async Task<int> InsertPurchaseReturn(PurchaseReturnModel purchaseReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseReturn, purchaseReturn, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Return.");

	private static async Task<int> InsertPurchaseReturnDetail(PurchaseReturnDetailModel purchaseReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseReturnDetail, purchaseReturnDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Return Detail.");

	public static List<PurchaseReturnDetailModel> ConvertCartToDetails(List<PurchaseReturnItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new PurchaseReturnDetailModel
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
		var purchaseReturns = await CommonData.LoadTableDataByFinancialAccountingId<PurchaseReturnModel>(InventoryNames.PurchaseReturn, financialAccountingId, sqlDataAccessTransaction);
		foreach (var purchaseReturn in purchaseReturns)
		{
			purchaseReturn.FinancialAccountingId = newFinancialAccountingId;
			await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
		}
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
		await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.PurchaseReturn), purchaseReturn.Id, sqlDataAccessTransaction);

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
		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, purchaseReturn.FinancialAccountingId ?? 0, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = purchaseReturn.LastModifiedBy;
		existingAccounting.LastModifiedAt = purchaseReturn.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = purchaseReturn.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(PurchaseReturnModel purchaseReturn)
	{
		purchaseReturn.Status = true;
		var purchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(InventoryNames.PurchaseReturnDetail, purchaseReturn.Id);
		await SaveTransaction(purchaseReturn, purchaseReturnDetails, true);

		await PurchaseReturnNotify.Notify(purchaseReturn.Id, NotifyType.Recovered);
	}

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
				?? throw new InvalidOperationException("The purchase return transaction does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPurchaseReturn.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, purchaseReturn.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users can update a purchase return transaction.");

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
			(MemoryStream, string)? previousInvoice = update && !recover ? await PurchaseReturnInvoiceExport.ExportInvoice(purchaseReturn.Id, InvoiceExportType.PDF) : null;

			purchaseReturn.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(purchaseReturn, purchaseReturnDetails, recover, transaction));

			if (!recover)
				await PurchaseReturnNotify.Notify(purchaseReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return purchaseReturn.Id;
		}

		purchaseReturn = await ValidateTransaction(purchaseReturn, update, sqlDataAccessTransaction);
		ValidateItemDetails(purchaseReturn, purchaseReturnDetails);

		var previousPurchaseReturn = update && !recover ? await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, purchaseReturn.Id, sqlDataAccessTransaction) : null;
		var previousPurchaseReturnDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, purchaseReturn.Id, sqlDataAccessTransaction) : null;

		purchaseReturn.Id = await InsertPurchaseReturn(purchaseReturn, sqlDataAccessTransaction);
		await SaveTransactionDetail(purchaseReturn, purchaseReturnDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(purchaseReturn, purchaseReturnDetails, update, sqlDataAccessTransaction);
		await SaveAccounting(purchaseReturn, update, sqlDataAccessTransaction);
		await SaveAuditTrail(purchaseReturn, update, recover, previousPurchaseReturn, previousPurchaseReturnDetails, sqlDataAccessTransaction);

		return purchaseReturn.Id;
	}

	private static async Task SaveTransactionDetail(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingPurchaseReturnDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(InventoryNames.PurchaseReturnDetail, purchaseReturn.Id, sqlDataAccessTransaction);
			foreach (var item in existingPurchaseReturnDetails)
			{
				item.Status = false;
				await InsertPurchaseReturnDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in purchaseReturnDetails)
		{
			item.MasterId = purchaseReturn.Id;
			await InsertPurchaseReturnDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStock(PurchaseReturnModel purchaseReturn, List<PurchaseReturnDetailModel> purchaseReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.PurchaseReturn), purchaseReturn.Id, sqlDataAccessTransaction);

		foreach (var item in purchaseReturnDetails)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				Type = nameof(StockType.PurchaseReturn),
				TransactionId = purchaseReturn.Id,
				TransactionNo = purchaseReturn.TransactionNo,
				TransactionDateTime = purchaseReturn.TransactionDateTime
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAccounting(PurchaseReturnModel purchaseReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId, sqlDataAccessTransaction);
			var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(purchaseReturnVoucher.Value), purchaseReturn.Id, purchaseReturn.TransactionNo, sqlDataAccessTransaction);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = purchaseReturn.LastModifiedBy;
				existingAccounting.LastModifiedAt = purchaseReturn.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = purchaseReturn.LastModifiedFromPlatform;

				await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
			}
		}

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

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
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
