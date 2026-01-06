using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Data.Sales.StockTransfer;

public static class StockTransferData
{
	private static async Task<int> InsertStockTransfer(StockTransferModel stockTransfer) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStockTransfer, stockTransfer)).FirstOrDefault();

	private static async Task<int> InsertStockTransferDetail(StockTransferDetailModel stockTransferDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStockTransferDetail, stockTransferDetail)).FirstOrDefault();

	public static async Task DeleteTransaction(StockTransferModel stockTransfer)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, stockTransfer.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

		stockTransfer.Status = false;
		await InsertStockTransfer(stockTransfer);

		await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), stockTransfer.Id, stockTransfer.LocationId);
		await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), stockTransfer.Id, stockTransfer.ToLocationId);
		await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.StockTransfer), stockTransfer.Id);

		var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);
		var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(stockTransferVoucher.Value), stockTransfer.Id, stockTransfer.TransactionNo);
		if (existingAccounting is not null && existingAccounting.Id > 0)
		{
			existingAccounting.Status = false;
			existingAccounting.LastModifiedBy = stockTransfer.LastModifiedBy;
			existingAccounting.LastModifiedAt = stockTransfer.LastModifiedAt;
			existingAccounting.LastModifiedFromPlatform = stockTransfer.LastModifiedFromPlatform;

			await AccountingData.DeleteTransaction(existingAccounting);
		}

		await SendNotification.StockTransferNotification(stockTransfer.Id, NotifyType.Deleted);
	}

	public static async Task RecoverTransaction(StockTransferModel stockTransfer)
	{
		var transactionDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, stockTransfer.Id);
		List<StockTransferItemCartModel> transactionItemCarts = [];

		foreach (var item in transactionDetails)
			transactionItemCarts.Add(new()
			{
				ItemId = item.ProductId,
				ItemName = "",
				Quantity = item.Quantity,
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
				InclusiveTax = item.InclusiveTax,
				TotalTaxAmount = item.TotalTaxAmount,
				Total = item.Total,
				NetRate = item.NetRate,
				Remarks = item.Remarks
			});

		await SaveTransaction(stockTransfer, transactionItemCarts, false);
		await SendNotification.StockTransferNotification(stockTransfer.Id, NotifyType.Recovered);
	}

	public static async Task<int> SaveTransaction(StockTransferModel stockTransfer, List<StockTransferItemCartModel> stockTransferDetails, bool showNotification = true)
	{
		bool update = stockTransfer.Id > 0;
		var existingStockTransfer = stockTransfer;

		if (update)
		{
			existingStockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, stockTransfer.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingStockTransfer.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			stockTransfer.TransactionNo = existingStockTransfer.TransactionNo;
		}
		else
			stockTransfer.TransactionNo = await GenerateCodes.GenerateStockTransferTransactionNo(stockTransfer);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, stockTransfer.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		stockTransfer.Id = await InsertStockTransfer(stockTransfer);
		await SaveTransactionDetail(stockTransfer, stockTransferDetails, update);
		await SaveProductStock(stockTransfer, stockTransferDetails, existingStockTransfer, update);
		await SaveRawMaterialStockByRecipe(stockTransfer, stockTransferDetails, existingStockTransfer, update);
		await SaveAccounting(stockTransfer, update);

		if (showNotification)
			await SendNotification.StockTransferNotification(stockTransfer.Id, update ? NotifyType.Updated : NotifyType.Created);

		return stockTransfer.Id;
	}

	private static async Task SaveTransactionDetail(StockTransferModel stockTransfer, List<StockTransferItemCartModel> stockTransferDetails, bool update)
	{
		if (update)
		{
			var existingStockTransferDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, stockTransfer.Id);
			foreach (var item in existingStockTransferDetails)
			{
				item.Status = false;
				await InsertStockTransferDetail(item);
			}
		}

		foreach (var item in stockTransferDetails)
			await InsertStockTransferDetail(new()
			{
				Id = 0,
				MasterId = stockTransfer.Id,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
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
			});
	}

	private static async Task SaveProductStock(StockTransferModel stockTransfer, List<StockTransferItemCartModel> cart, StockTransferModel existingStockTransfer, bool update)
	{
		if (update)
		{
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), existingStockTransfer.Id, existingStockTransfer.ToLocationId);
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.StockTransfer), existingStockTransfer.Id, existingStockTransfer.LocationId);
		}

		// From Location Stock Update
		foreach (var item in cart)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = stockTransfer.Id,
				Type = nameof(StockType.StockTransfer),
				TransactionNo = stockTransfer.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime),
				LocationId = stockTransfer.LocationId
			});

		// To Location Stock Update
		foreach (var item in cart)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				TransactionId = stockTransfer.Id,
				Type = nameof(StockType.StockTransfer),
				TransactionNo = stockTransfer.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime),
				LocationId = stockTransfer.ToLocationId
			});
	}

	private static async Task SaveRawMaterialStockByRecipe(StockTransferModel stockTransfer, List<StockTransferItemCartModel> cart, StockTransferModel existingStockTransfer, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.StockTransfer), existingStockTransfer.Id);

		if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
			return;

		foreach (var product in cart)
		{
			var recipe = await RecipeData.LoadRecipeByProduct(product.ItemId);
			var recipeItems = recipe is null ? [] : await RecipeData.LoadRecipeDetailByRecipe(recipe.Id);

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = stockTransfer.LocationId == 1 ? -recipeItem.Quantity * product.Quantity : recipeItem.Quantity * product.Quantity,
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = stockTransfer.Id,
					TransactionNo = stockTransfer.TransactionNo,
					Type = nameof(StockType.StockTransfer),
					TransactionDate = DateOnly.FromDateTime(stockTransfer.TransactionDateTime)
				});
		}
	}

	private static async Task SaveAccounting(StockTransferModel stockTransfer, bool update)
	{
		if (update)
		{
			var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);
			var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(stockTransferVoucher.Value), stockTransfer.Id, stockTransfer.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = stockTransfer.LastModifiedBy;
				existingAccounting.LastModifiedAt = stockTransfer.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = stockTransfer.LastModifiedFromPlatform;

				await AccountingData.DeleteTransaction(existingAccounting);
			}
		}

		if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
			return;

		var stockTransferOverview = await CommonData.LoadTableDataById<StockTransferOverviewModel>(ViewNames.StockTransferOverview, stockTransfer.Id);
		if (stockTransferOverview is null)
			return;

		if (stockTransferOverview.TotalAmount == 0)
			return;

		var accountingCart = new List<AccountingItemCartModel>();

		if (stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card > 0)
		{
			var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(ReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(cashLedger.Value),
				Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
				Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
				Remarks = $"Cash Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});
		}

		if (stockTransferOverview.Credit > 0)
		{
			var ledger = await LedgerData.LoadLedgerByLocation(stockTransferOverview.ToLocationId);
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(ReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = ledger.Id,
				Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Credit : null,
				Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Credit : null,
				Remarks = $"Party Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});
		}

		if (stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount > 0)
		{
			var stockTransferLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(ReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(stockTransferLedger.Value),
				Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
				Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
				Remarks = $"Stock Transfer Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});
		}

		if (stockTransferOverview.TotalExtraTaxAmount > 0)
		{
			var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(ReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
				Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
				Remarks = $"GST Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});
		}

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);
		var accounting = new AccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = stockTransferOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = stockTransferOverview.Id,
			ReferenceNo = stockTransferOverview.TransactionNo,
			TransactionDateTime = stockTransferOverview.TransactionDateTime,
			FinancialYearId = stockTransferOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = stockTransferOverview.Remarks,
			CreatedBy = stockTransferOverview.CreatedBy,
			CreatedAt = stockTransferOverview.CreatedAt,
			CreatedFromPlatform = stockTransferOverview.CreatedFromPlatform,
			Status = true
		};

		await AccountingData.SaveTransaction(accounting, accountingCart);
	}
}
