using PrimeBakesLibrary.Accounts.FinancialAccounting.Data;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Recipe.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.StockTransfer.Exports;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Store.StockTransfer.Data;

public static class StockTransferData
{
	private static async Task<int> InsertStockTransfer(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertStockTransfer, stockTransfer, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Stock Transfer.");

	private static async Task<int> InsertStockTransferDetail(StockTransferDetailModel stockTransferDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertStockTransferDetail, stockTransferDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Stock Transfer Detail.");

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var stockTransfers = await CommonData.LoadTableDataByFinancialAccountingId<StockTransferModel>(StoreNames.StockTransfer, financialAccountingId, sqlDataAccessTransaction);
		foreach (var stockTransfer in stockTransfers)
		{
			stockTransfer.FinancialAccountingId = newFinancialAccountingId;
			await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);
		}
	}

	public static List<StockTransferDetailModel> ConvertCartToDetails(List<StockTransferItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new StockTransferDetailModel
		{
			Id = 0,
			MasterId = masterId,
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
		})];

	#region Delete
	public static async Task DeleteTransaction(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(stockTransfer, transaction));
			await StockTransferNotify.Notify(stockTransfer.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(stockTransfer.TransactionDateTime, sqlDataAccessTransaction);

		stockTransfer.Status = false;
		await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);

		await ProductStockData.DeleteProductStockByTransactionNo(stockTransfer.TransactionNo, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(stockTransfer.TransactionNo, sqlDataAccessTransaction);
		await DeleteAccounting(stockTransfer, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = StoreNames.StockTransfer,
			RecordNo = stockTransfer.TransactionNo,
			CreatedBy = stockTransfer.LastModifiedBy.Value,
			CreatedFromPlatform = stockTransfer.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (stockTransfer.FinancialAccountingId is null || stockTransfer.FinancialAccountingId <= 0)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, stockTransfer.FinancialAccountingId.Value, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = stockTransfer.LastModifiedBy;
		existingAccounting.LastModifiedAt = stockTransfer.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = stockTransfer.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(StockTransferModel stockTransfer)
	{
		stockTransfer.Status = true;
		var stockTransferDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(StoreNames.StockTransferDetail, stockTransfer.Id);
		await SaveTransaction(stockTransfer, stockTransferDetails, true);

		await StockTransferNotify.Notify(stockTransfer.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<StockTransferModel> ValidateTransaction(StockTransferModel stockTransfer, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		stockTransfer.Remarks = string.IsNullOrWhiteSpace(stockTransfer.Remarks) ? null : stockTransfer.Remarks.Trim();

		if (stockTransfer.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (stockTransfer.LocationId <= 0 || stockTransfer.ToLocationId <= 0)
			throw new InvalidOperationException("Please select both the from and to locations for the transaction.");

		if (stockTransfer.LocationId == stockTransfer.ToLocationId)
			throw new InvalidOperationException("The from and to locations cannot be the same.");

		if (stockTransfer.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (stockTransfer.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (stockTransfer.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (stockTransfer.Cash < 0 || stockTransfer.Card < 0 || stockTransfer.Credit < 0 || stockTransfer.UPI < 0)
			throw new InvalidOperationException("Payment amounts cannot be negative.");

		if (stockTransfer.Cash + stockTransfer.Card + stockTransfer.Credit + stockTransfer.UPI != stockTransfer.TotalAmount)
			throw new InvalidOperationException("The sum of all payments must equal the total amount of the transaction.");

		var userId = update ? stockTransfer.LastModifiedBy : stockTransfer.CreatedBy;
		var stockTransferUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);

		if (!stockTransferUser.ChangeProductFinancial)
		{
			if (stockTransfer.ItemDiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			if (stockTransfer.OtherChargesPercent != 0 || stockTransfer.OtherChargesAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply other charges.");

			if (stockTransfer.DiscountPercent != 0 || stockTransfer.DiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply discount.");
		}

		if (update)
		{
			var existingStockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(StoreNames.StockTransfer, stockTransfer.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingStockTransfer.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, stockTransfer.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			stockTransfer.TransactionNo = existingStockTransfer.TransactionNo;
		}
		else
			stockTransfer.TransactionNo = await GenerateCodes.GenerateStockTransferTransactionNo(stockTransfer, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(stockTransfer.TransactionDateTime, sqlDataAccessTransaction);

		return stockTransfer;
	}

	private static async Task ValidateItemDetails(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		foreach (var item in stockTransferDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (stockTransferDetails is null || stockTransferDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (stockTransferDetails.Any(ed => ed.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (stockTransferDetails.Any(ed => !ed.Status))
			throw new InvalidOperationException("Stock transfer detail items must be active.");

		if (stockTransferDetails.Count != stockTransfer.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (stockTransferDetails.Sum(ed => ed.Quantity) != stockTransfer.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (stockTransferDetails.Sum(ed => ed.BaseTotal) != stockTransfer.BaseTotal)
			throw new InvalidOperationException("Base total must be equal to the sum of item base totals.");

		if (stockTransferDetails.Sum(ed => ed.DiscountAmount) != stockTransfer.ItemDiscountAmount)
			throw new InvalidOperationException("Item discount amount must be equal to the sum of item discount amounts.");

		if (stockTransferDetails.Sum(ed => ed.AfterDiscount) != stockTransfer.TotalAfterItemDiscount)
			throw new InvalidOperationException("Total after item discount must be equal to the sum of item totals after discount.");

		if (stockTransferDetails.Sum(ed => ed.TotalTaxAmount) != stockTransfer.TotalInclusiveTaxAmount + stockTransfer.TotalExtraTaxAmount)
			throw new InvalidOperationException("Total tax amount must be equal to the sum of inclusive and extra tax amounts.");

		if (stockTransferDetails.Sum(ed => ed.Total) != stockTransfer.TotalAfterTax)
			throw new InvalidOperationException("Total after tax must be equal to the sum of item totals.");

		var userId = update ? stockTransfer.LastModifiedBy : stockTransfer.CreatedBy;
		var stockTransferUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);
		if (!stockTransferUser.ChangeProductFinancial)
		{
			if (stockTransferDetails.Any(ed => ed.DiscountAmount != 0 || ed.DiscountPercent != 0))
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: stockTransfer.LocationId, sqlDataAccessTransaction: sqlDataAccessTransaction);
			var taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax, sqlDataAccessTransaction);

			foreach (var item in stockTransferDetails)
			{
				var product = productLocations.FirstOrDefault(pl => pl.ProductId == item.ProductId)
					?? throw new InvalidOperationException($"Product with ID '{item.ProductId}' is not available in the selected location.");

				var tax = taxes.FirstOrDefault(t => t.Id == product.TaxId)
					?? throw new InvalidOperationException($"Tax information for product '{product.Name}' is not available.");

				if (item.Rate != product.Rate)
					throw new InvalidOperationException($"You are not allowed to change the rate for product '{product.Name}'.");

				if (item.CGSTPercent != tax.CGST || item.SGSTPercent != tax.SGST)
					throw new InvalidOperationException($"You are not allowed to change the tax for product '{product.Name}'.");

				if (item.InclusiveTax != tax.Inclusive)
					throw new InvalidOperationException($"You are not allowed to change the tax inclusion for product '{product.Name}'.");
			}
		}
	}

	public static async Task<int> SaveTransaction(
		StockTransferModel stockTransfer,
		List<StockTransferDetailModel> stockTransferDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = stockTransfer.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await StockTransferInvoiceExport.ExportInvoice(stockTransfer.Id, InvoiceExportType.PDF) : null;

			stockTransfer.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(stockTransfer, stockTransferDetails, recover, transaction));

			if (!recover)
				await StockTransferNotify.Notify(stockTransfer.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return stockTransfer.Id;
		}

		stockTransfer = await ValidateTransaction(stockTransfer, update, sqlDataAccessTransaction);
		await ValidateItemDetails(stockTransfer, stockTransferDetails, update, sqlDataAccessTransaction);

		var previousStockTransfer = update && !recover ? await CommonData.LoadTableDataById<StockTransferOverviewModel>(StoreNames.StockTransferOverview, stockTransfer.Id, sqlDataAccessTransaction) : new();
		var previousStockTransferDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, stockTransfer.Id, sqlDataAccessTransaction) : [];

		stockTransfer.Id = await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);
		await SaveTransactionDetail(stockTransfer, stockTransferDetails, update, sqlDataAccessTransaction);
		await SaveProductStock(stockTransfer, stockTransferDetails, sqlDataAccessTransaction);
		await SaveRawMaterialStockByRecipe(stockTransfer, stockTransferDetails, sqlDataAccessTransaction);
		await SaveAccounting(stockTransfer, sqlDataAccessTransaction);
		await SaveAuditTrail(stockTransfer, update, recover, previousStockTransfer, previousStockTransferDetails, sqlDataAccessTransaction);

		return stockTransfer.Id;
	}

	private static async Task SaveTransactionDetail(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingStockTransferDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(StoreNames.StockTransferDetail, stockTransfer.Id, sqlDataAccessTransaction);
			foreach (var item in existingStockTransferDetails)
			{
				item.Status = false;
				await InsertStockTransferDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in stockTransferDetails)
		{
			item.MasterId = stockTransfer.Id;
			await InsertStockTransferDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(stockTransfer.TransactionNo, sqlDataAccessTransaction);

		// From Location Stock Update (negative quantity - stock leaves)
		foreach (var item in stockTransferDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = stockTransfer.Id,
				Type = nameof(StockType.StockTransfer),
				TransactionNo = stockTransfer.TransactionNo,
				TransactionDateTime = stockTransfer.TransactionDateTime,
				LocationId = stockTransfer.LocationId
			}, sqlDataAccessTransaction);

		// To Location Stock Update (positive quantity - stock arrives)
		foreach (var item in stockTransferDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				TransactionId = stockTransfer.Id,
				Type = nameof(StockType.StockTransfer),
				TransactionNo = stockTransfer.TransactionNo,
				TransactionDateTime = stockTransfer.TransactionDateTime,
				LocationId = stockTransfer.ToLocationId
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveRawMaterialStockByRecipe(StockTransferModel stockTransfer, List<StockTransferDetailModel> stockTransferDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(stockTransfer.TransactionNo, sqlDataAccessTransaction);

		if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
			return;

		var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(InventoryNames.Recipe, true, sqlDataAccessTransaction);
		recipes = [.. recipes.Where(r => r.Deduct)];
		var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(InventoryNames.RecipeDetail, true, sqlDataAccessTransaction);

		foreach (var product in stockTransferDetails)
		{
			var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
			var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = stockTransfer.LocationId == 1 ? -recipeItem.Quantity * (product.Quantity / recipe.Quantity) : recipeItem.Quantity * (product.Quantity / recipe.Quantity),
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = stockTransfer.Id,
					TransactionNo = stockTransfer.TransactionNo,
					Type = nameof(StockType.StockTransfer),
					TransactionDateTime = stockTransfer.TransactionDateTime
				}, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAccounting(StockTransferModel stockTransfer, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await DeleteAccounting(stockTransfer, sqlDataAccessTransaction);

		if (stockTransfer.LocationId != 1 && stockTransfer.ToLocationId != 1)
			return;

		var stockTransferOverview = await CommonData.LoadTableDataById<StockTransferOverviewModel>(StoreNames.StockTransferOverview, stockTransfer.Id, sqlDataAccessTransaction);
		if (stockTransferOverview is null || stockTransferOverview.TotalAmount == 0)
			return;

		var ledger = await LocationData.LoadLedgerByLocationId(stockTransferOverview.ToLocationId, sqlDataAccessTransaction);
		var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
		var stockTransferLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card > 0)
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(cashLedger.Value),
				Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
				Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Cash + stockTransferOverview.UPI + stockTransferOverview.Card : null,
				Remarks = $"Cash Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});

		if (stockTransferOverview.Credit > 0)
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = ledger.Id,
				Debit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.Credit : null,
				Credit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.Credit : null,
				Remarks = $"Party Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});

		if (stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(stockTransferLedger.Value),
				Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
				Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalAmount - stockTransferOverview.TotalExtraTaxAmount : null,
				Remarks = $"Stock Transfer Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});

		if (stockTransferOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = stockTransferOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.StockTransfer),
				ReferenceNo = stockTransferOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = stockTransferOverview.ToLocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
				Credit = stockTransferOverview.LocationId == 1 ? stockTransferOverview.TotalExtraTaxAmount : null,
				Remarks = $"GST Account Posting For Stock Transfer {stockTransferOverview.TransactionNo}",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
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

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
		accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		stockTransfer.FinancialAccountingId = accounting.Id;
		await InsertStockTransfer(stockTransfer, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		StockTransferModel stockTransfer,
		bool update,
		bool recover,
		StockTransferOverviewModel previousStockTransfer = null,
		List<StockTransferItemOverviewModel> previousStockTransferDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentStockTransfer = await CommonData.LoadTableDataById<StockTransferOverviewModel>(StoreNames.StockTransferOverview, stockTransfer.Id, sqlDataAccessTransaction);
			var currentStockTransferDetails = await CommonData.LoadTableDataByMasterId<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, stockTransfer.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousStockTransfer, currentStockTransfer);
			var detailsDiff = AuditTrailData.GetDifference(previousStockTransferDetails, currentStockTransferDetails, typeof(StockTransferOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = StoreNames.StockTransfer,
			RecordNo = stockTransfer.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? stockTransfer.LastModifiedBy.Value : stockTransfer.CreatedBy,
			CreatedFromPlatform = update ? stockTransfer.LastModifiedFromPlatform : stockTransfer.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
