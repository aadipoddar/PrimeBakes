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
using PrimeBakesLibrary.Models.Store.Product;
using PrimeBakesLibrary.Models.Store.Sale;

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

	private static async Task<List<BillModel>> LoadBillByFinancialAccountingId(int FinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<BillModel, dynamic>(StoredProcedureNames.LoadBillByFinancialAccountingId, new { FinancialAccountingId }, sqlDataAccessTransaction);

	public static async Task<Dictionary<int, List<BillItemCartModel>>> KOTCategoryItemsFromBill(int billId)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, billId);
		var kotItems = billDetails.Where(item => item.KOTPrint).ToList();

		if (kotItems.Count == 0)
			return [];

		var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
		var kotProducts = allProducts.Where(p => kotItems.Any(ki => ki.ProductId == p.Id)).ToList();
		var kotCategoryIds = kotProducts.Select(p => p.KOTCategoryId).Distinct().ToList();

		var result = new Dictionary<int, List<BillItemCartModel>>();

		foreach (var kotCategoryId in kotCategoryIds)
		{
			var categoryProducts = kotProducts.Where(p => p.KOTCategoryId == kotCategoryId).ToList();
			result[kotCategoryId] = [];

			foreach (var product in categoryProducts)
			{
				var items = kotItems.Where(ki => ki.ProductId == product.Id).ToList();
				foreach (var item in items)
					result[kotCategoryId].Add(new BillItemCartModel
					{
						ItemCategoryId = product.KOTCategoryId,
						ItemId = product.Id,
						ItemName = product.Name,
						Quantity = item.Quantity,
						Remarks = item.Remarks,
						KOTPrint = item.KOTPrint
					});
			}
		}

		return result;
	}

	public static async Task MarkKOTAsPrinted(int billId)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, billId);

		foreach (var detail in billDetails.Where(d => d.KOTPrint))
		{
			detail.KOTPrint = false;
			await InsertBillDetail(detail);
		}
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

	public static async Task DeleteTransaction(BillModel bill)
	{
		if (bill.FinancialAccountingId is not null)
			throw new InvalidOperationException("Cannot delete a bill with financial accounting.");

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

			if (existingBill.FinancialAccountingId is not null)
				throw new InvalidOperationException("Cannot update a bill with financial accounting.");

			await ValidateDayBillsAccountPosting(existingBill.TransactionDateTime, existingBill.LocationId, sqlDataAccessTransaction);
			await FinancialYearData.ValidateFinancialYear(existingBill.TransactionDateTime, sqlDataAccessTransaction);
			bill.TransactionNo = existingBill.TransactionNo;
			previousRunning = existingBill.Running;
		}
		else
			bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(bill, sqlDataAccessTransaction);

		await ValidateDayBillsAccountPosting(existingBill.TransactionDateTime, bill.LocationId, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime, sqlDataAccessTransaction);

		bill.Id = await InsertBill(bill, sqlDataAccessTransaction);
		billDetails ??= ConvertCartToDetails(cart, bill.Id);
		await SaveTransactionDetail(bill, billDetails, update, previousRunning, sqlDataAccessTransaction);

		if (!bill.Running)
		{
			await SaveProductStock(bill, billDetails, existingBill, update, sqlDataAccessTransaction);
			await SaveRawMaterialStockByRecipe(bill, billDetails, existingBill, update, sqlDataAccessTransaction);

			if (bill.LocationId == 1)
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
				TransactionDateTime = bill.TransactionDateTime,
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
					TransactionDateTime = bill.TransactionDateTime
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
				Remarks = $"Bill Account Posting For Bill {billOverview.TransactionNo}",
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

	public static async Task PostDayBills(DateTime postingDate, int locationId, int userId, string userPlatform)
	{
		await ValidateDayBillsAccountPosting(postingDate, locationId);
		await FinancialYearData.ValidateFinancialYear(postingDate);

		var bills = await CommonData.LoadTableDataByDate<BillOverviewModel>(
				ViewNames.BillOverview,
				DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MaxValue));

		bills = [.. bills.Where(b =>
									b.LocationId == locationId &&
									b.FinancialAccountingTransactionNo is null &&
									b.Status &&
									!b.Running)];

		if (bills.Count == 0)
			return;

		var accountingCart = new List<FinancialAccountingItemCartModel>();

		var totalAmount = bills.Sum(b => b.Cash + b.UPI + b.Card + b.Credit);
		var totalExtraTaxAmount = bills.Sum(b => b.TotalExtraTaxAmount);

		if (totalAmount <= 0)
			return;

		var ledger = await LedgerData.LoadLedgerByLocationId(locationId);
		accountingCart.Add(new()
		{
			LedgerId = ledger.Id,
			Debit = totalAmount,
			Credit = null,
			Remarks = "Location Account Posting For Bill Day Closing",
		});

		if (totalAmount - totalExtraTaxAmount > 0)
		{
			var billLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.BillLedgerId);
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(billLedger.Value),
				Debit = null,
				Credit = totalAmount - totalExtraTaxAmount,
				Remarks = "Bill Account Posting For Bill Day Closing",
			});
		}

		if (totalExtraTaxAmount > 0)
		{
			var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = totalExtraTaxAmount,
				Remarks = "GST Account Posting For Bill Day Closing",
			});
		}

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.BillDayCloseVoucherId);
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = bills.First().CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = null,
			ReferenceNo = null,
			TransactionDateTime = DateOnly.FromDateTime(postingDate).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second)),
			FinancialYearId = financialYear.Id,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = $"Bill Day Closing for {postingDate:dd-MMM-yyyy}",
			CreatedBy = userId,
			CreatedAt = currentDateTime,
			CreatedFromPlatform = userPlatform,
			Status = true
		};

		using SqlDataAccessTransaction sqlDataAccessTransaction = new();

		try
		{
			sqlDataAccessTransaction.StartTransaction();

			accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, accountingCart, null, false, sqlDataAccessTransaction);

			foreach (var billOverview in bills)
			{
				var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, billOverview.Id, sqlDataAccessTransaction);
				bill.FinancialAccountingId = accounting.Id;
				await InsertBill(bill, sqlDataAccessTransaction);
			}

			sqlDataAccessTransaction.CommitTransaction();
		}
		catch
		{
			sqlDataAccessTransaction.RollbackTransaction();
			throw;
		}


		await BillNotify.NotifyDayClosing(
			postingDate,
			locationId,
			bills.Count,
			totalAmount,
			totalExtraTaxAmount,
			userId,
			accounting.TransactionNo);
	}

	internal static async Task UpdateBillsFinancialAccountingId(int financialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var bills = await LoadBillByFinancialAccountingId(financialAccountingId, sqlDataAccessTransaction);
		foreach (var bill in bills)
		{
			bill.FinancialAccountingId = null;
			await InsertBill(bill, sqlDataAccessTransaction);
		}
	}

	private static async Task ValidateDayBillsAccountPosting(DateTime postDate, int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var bills = await CommonData.LoadTableDataByDate<BillModel>(
			TableNames.Bill,
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MaxValue),
			sqlDataAccessTransaction);

		bills = [.. bills.Where(b => b.LocationId == locationId && b.FinancialAccountingId is not null && b.Status)];
		if (bills.Count > 0)
			throw new InvalidOperationException("Cannot post day bills account entry as there are already posted bills for the day. Please contact administrator.");
	}
}