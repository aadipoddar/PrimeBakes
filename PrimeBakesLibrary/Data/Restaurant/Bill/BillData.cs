using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;

namespace PrimeBakesLibrary.Data.Restaurant.Bill;

public static class BillData
{
	private static async Task<int> InsertBill(BillModel bill, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertBill, bill, sqlDataAccessTransaction)).FirstOrDefault();

	private static async Task<int> InsertBillDetail(BillDetailModel billDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertBillDetail, billDetail, sqlDataAccessTransaction)).FirstOrDefault();

	private static async Task DeleteBillDetailById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteBillDetailById, new { Id }, sqlDataAccessTransaction);

	public static async Task<List<BillModel>> LoadRunningBillByLocationId(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<BillModel, dynamic>(StoredProcedureNames.LoadRunningBillByLocationId, new { LocationId }, sqlDataAccessTransaction);

	public static async Task MarkKOTAsPrinted(int billId)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, billId);

		foreach (var detail in billDetails.Where(d => d.KOTPrint))
		{
			detail.KOTPrint = false;
			await InsertBillDetail(detail);
		}
	}

	public static async Task DeleteTransaction(BillModel bill)
	{
		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime);

		using SqlDataAccessTransaction sqlDataAccessTransaction = new();

		try
		{
			sqlDataAccessTransaction.StartTransaction();

			bill.Status = false;
			await InsertBill(bill, sqlDataAccessTransaction);

			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Bill), bill.Id, bill.LocationId, sqlDataAccessTransaction);
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Bill), bill.Id, sqlDataAccessTransaction);

			var billVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.BillVoucherId, sqlDataAccessTransaction);
			var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(billVoucher.Value), bill.Id, bill.TransactionNo, sqlDataAccessTransaction);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = bill.LastModifiedBy;
				existingAccounting.LastModifiedAt = bill.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = bill.LastModifiedFromPlatform;

				await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
			}

			sqlDataAccessTransaction.CommitTransaction();

			await BillNotify.Notify(bill.Id, NotifyType.Deleted);
		}
		catch
		{
			sqlDataAccessTransaction.RollbackTransaction();
			throw;
		}
	}

	public static async Task RecoverTransaction(BillModel bill)
	{
		bill.Status = true;
		var transactionDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);

		await SaveTransaction(bill, null, transactionDetails, false);
		await BillNotify.Notify(bill.Id, NotifyType.Recovered);
	}

	public static List<BillDetailModel> ConvertCartToDetails(List<BillItemCartModel> cart, int masterId) =>
		[.. cart.Select(item => new BillDetailModel
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
			KOTPrint = item.KOTPrint,
			Status = true
		})];

	public static async Task<int> SaveTransaction(BillModel bill, List<BillItemCartModel> cart = null, List<BillDetailModel> billDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = bill.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = null;
			if (update && !bill.Running)
				previousInvoice = await BillInvoiceExport.ExportInvoice(bill.Id, InvoiceExportType.PDF);

			using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

			try
			{
				newSqlDataAccessTransaction.StartTransaction();
				bill.Id = await SaveTransaction(bill, cart, billDetails, showNotification, newSqlDataAccessTransaction);
				newSqlDataAccessTransaction.CommitTransaction();
			}
			catch
			{
				newSqlDataAccessTransaction.RollbackTransaction();
				throw;
			}

			if (showNotification && !bill.Running)
				await BillNotify.Notify(bill.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return bill.Id;
		}

		var existingBill = bill;
		var previousRunning = true;

		if (update)
		{
			existingBill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, bill.Id, sqlDataAccessTransaction);
			await FinancialYearData.ValidateFinancialYear(existingBill.TransactionDateTime, sqlDataAccessTransaction);
			bill.TransactionNo = existingBill.TransactionNo;
			previousRunning = existingBill.Running;
		}
		else
			bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(bill, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime, sqlDataAccessTransaction);

		bill.Id = await InsertBill(bill, sqlDataAccessTransaction);
		billDetails ??= ConvertCartToDetails(cart, bill.Id);
		await SaveTransactionDetail(bill, billDetails, update, previousRunning, sqlDataAccessTransaction);

		if (!bill.Running)
		{
			await SaveProductStock(bill, billDetails, existingBill, update, sqlDataAccessTransaction);
			await SaveRawMaterialStockByRecipe(bill, billDetails, existingBill, update, sqlDataAccessTransaction);
			await SaveAccounting(bill, update, sqlDataAccessTransaction);
		}

		return bill.Id;
	}

	private static async Task SaveTransactionDetail(BillModel bill, List<BillDetailModel> billDetails, bool update, bool previousRunning, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (billDetails is null || billDetails.Count != bill.TotalItems || billDetails.Sum(d => d.Quantity) != bill.TotalQuantity)
			throw new InvalidOperationException("Bill details do not match the transaction summary.");

		if (billDetails.Any(d => !d.Status))
			throw new InvalidOperationException("Bill detail items must be active.");

		if (update)
		{
			var existingBillDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id, sqlDataAccessTransaction);
			foreach (var item in existingBillDetails)
			{
				if (bill.Running || previousRunning)
					await DeleteBillDetailById(item.Id, sqlDataAccessTransaction);

				else
				{
					item.Status = false;
					await InsertBillDetail(item, sqlDataAccessTransaction);
				}
			}
		}

		foreach (var item in billDetails)
		{
			item.MasterId = bill.Id;
			var id = await InsertBillDetail(item, sqlDataAccessTransaction);

			if (id <= 0)
				throw new InvalidOperationException("Failed to save bill detail item.");
		}
	}

	private static async Task SaveProductStock(BillModel bill, List<BillDetailModel> billDetails, BillModel existingBill, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.Bill), existingBill.Id, existingBill.LocationId, sqlDataAccessTransaction);

		foreach (var item in billDetails)
		{
			var id = await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = bill.Id,
				Type = nameof(StockType.Bill),
				TransactionNo = bill.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(bill.TransactionDateTime),
				LocationId = bill.LocationId
			}, sqlDataAccessTransaction);

			if (id <= 0)
				throw new InvalidOperationException("Failed to save product stock for bill.");
		}
	}

	private static async Task SaveRawMaterialStockByRecipe(BillModel bill, List<BillDetailModel> billDetails, BillModel existingBill, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.Bill), existingBill.Id, sqlDataAccessTransaction);

		if (bill.LocationId != 1)
			return;

		var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(TableNames.Recipe, true, sqlDataAccessTransaction);
		var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(TableNames.RecipeDetail, true, sqlDataAccessTransaction);

		foreach (var product in billDetails)
		{
			var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
			var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

			foreach (var recipeItem in recipeItems)
			{
				var id = await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = -recipeItem.Quantity * product.Quantity,
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = bill.Id,
					TransactionNo = bill.TransactionNo,
					Type = nameof(StockType.Bill),
					TransactionDate = DateOnly.FromDateTime(bill.TransactionDateTime)
				}, sqlDataAccessTransaction);

				if (id <= 0)
					throw new InvalidOperationException("Failed to save raw material stock for bill.");
			}
		}
	}

	private static async Task SaveAccounting(BillModel bill, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var billVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.BillVoucherId, sqlDataAccessTransaction);
			var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(billVoucher.Value), bill.Id, bill.TransactionNo, sqlDataAccessTransaction);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = bill.LastModifiedBy;
				existingAccounting.LastModifiedAt = bill.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = bill.LastModifiedFromPlatform;

				await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
			}
		}

		var billOverview = await CommonData.LoadTableDataById<BillOverviewModel>(ViewNames.BillOverview, bill.Id, sqlDataAccessTransaction);
		if (billOverview is null)
			return;

		if (billOverview.TotalAmount == 0)
			return;

		var accountingCart = new List<FinancialAccountingItemCartModel>();

		if (bill.LocationId == 1)
		{
			if (billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit > 0)
			{
				var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
				accountingCart.Add(new()
				{
					ReferenceId = billOverview.Id,
					ReferenceType = nameof(ReferenceTypes.Bill),
					ReferenceNo = billOverview.TransactionNo,
					LedgerId = int.Parse(cashLedger.Value),
					Debit = billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit,
					Credit = null,
					Remarks = $"Cash Account Posting For Bill {billOverview.TransactionNo}",
				});
			}
		}
		else
		{
			if (billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit > 0)
			{
				var ledger = await LedgerData.LoadLedgerByLocationId(bill.LocationId, sqlDataAccessTransaction);
				accountingCart.Add(new()
				{
					ReferenceId = billOverview.Id,
					ReferenceType = nameof(ReferenceTypes.Bill),
					ReferenceNo = billOverview.TransactionNo,
					LedgerId = ledger.Id,
					Debit = billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit,
					Credit = null,
					Remarks = $"Location Account Posting For Bill {billOverview.TransactionNo}",
				});
			}
		}

		if (billOverview.TotalAmount - billOverview.TotalExtraTaxAmount > 0)
		{
			var billLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.BillLedgerId, sqlDataAccessTransaction);
			accountingCart.Add(new()
			{
				ReferenceId = billOverview.Id,
				ReferenceType = nameof(ReferenceTypes.Bill),
				ReferenceNo = billOverview.TransactionNo,
				LedgerId = int.Parse(billLedger.Value),
				Debit = null,
				Credit = billOverview.TotalAmount - billOverview.TotalExtraTaxAmount,
				Remarks = $"Sale Account Posting For Bill {billOverview.TransactionNo}",
			});
		}

		if (billOverview.TotalExtraTaxAmount > 0)
		{
			var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
			accountingCart.Add(new()
			{
				ReferenceId = billOverview.Id,
				ReferenceType = nameof(ReferenceTypes.Bill),
				ReferenceNo = billOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = billOverview.TotalExtraTaxAmount,
				Remarks = $"GST Account Posting For Bill {billOverview.TransactionNo}",
			});
		}

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.BillVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = billOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = billOverview.Id,
			ReferenceNo = billOverview.TransactionNo,
			TransactionDateTime = billOverview.TransactionDateTime,
			FinancialYearId = billOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = billOverview.Remarks,
			CreatedBy = billOverview.CreatedBy,
			CreatedAt = billOverview.CreatedAt,
			CreatedFromPlatform = billOverview.CreatedFromPlatform,
			Status = true
		};

		await FinancialAccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);
	}
}