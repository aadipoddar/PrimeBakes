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
using PrimeBakesLibrary.Restaurant.Bill.Exports;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Restaurant.Dining.Models;
using PrimeBakesLibrary.Store.Customer;
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Restaurant.Bill.Data;

public static class BillData
{
	private static async Task<int> InsertBill(BillModel bill, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertBill, bill, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Bill.");

	private static async Task<int> InsertBillDetail(BillDetailModel billDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertBillDetail, billDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Bill Detail.");

	private static async Task<int> DeleteBillDetailById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.DeleteBillDetailById, new { Id }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Bill Detail.");

	public static async Task<List<BillModel>> LoadRunningBillByLocationId(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<BillModel, dynamic>(RestaurantNames.LoadRunningBillByLocationId, new { LocationId }, sqlDataAccessTransaction);

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var bills = await CommonData.LoadTableDataByFinancialAccountingId<BillModel>(RestaurantNames.Bill, financialAccountingId, sqlDataAccessTransaction);
		foreach (var bill in bills)
		{
			bill.FinancialAccountingId = newFinancialAccountingId;
			await InsertBill(bill, sqlDataAccessTransaction);
		}
	}

	public static List<BillDetailModel> ConvertCartToDetails(List<BillItemCartModel> cart, int masterId = 0) =>
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

	#region KOT
	public static async Task<Dictionary<int, List<BillItemCartModel>>> KOTCategoryItemsFromBill(int billId)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, billId);
		var kotItems = billDetails.Where(item => item.KOTPrint).ToList();

		if (kotItems.Count == 0)
			return [];

		var allProducts = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);
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
					result[kotCategoryId].Add(new()
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
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, billId);

		foreach (var detail in billDetails.Where(d => d.KOTPrint))
		{
			detail.KOTPrint = false;
			await InsertBillDetail(detail);
		}
	}
	#endregion

	#region Delete
	public static async Task DeleteTransaction(BillModel bill, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(bill, transaction));
			await BillNotify.Notify(bill.Id, NotifyType.Deleted);
			return;
		}

		await ValidateDayBillsAccountPosting(bill.TransactionDateTime, bill.LocationId, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime, sqlDataAccessTransaction);

		bill.Status = false;
		await InsertBill(bill, sqlDataAccessTransaction);

		await ProductStockData.DeleteProductStockByTransactionNo(bill.TransactionNo, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(bill.TransactionNo, sqlDataAccessTransaction);
		await DeleteAccounting(bill, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = RestaurantNames.Bill,
			RecordNo = bill.TransactionNo,
			CreatedBy = bill.LastModifiedBy.Value,
			CreatedFromPlatform = bill.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(BillModel bill, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (bill.FinancialAccountingId is null || bill.FinancialAccountingId <= 0)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, bill.FinancialAccountingId.Value, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = bill.LastModifiedBy;
		existingAccounting.LastModifiedAt = bill.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = bill.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(BillModel bill)
	{
		bill.Status = true;
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, bill.Id);
		await SaveTransaction(bill, billDetails, recover: true);

		await BillNotify.Notify(bill.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<int?> ResolveCustomer(CustomerModel customer, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (customer is null || customer.Id > 0)
			return customer?.Id is > 0 ? customer.Id : null;

		customer.Number = string.IsNullOrWhiteSpace(customer.Number) ? null : customer.Number.Trim();
		customer.Name = string.IsNullOrWhiteSpace(customer.Name) ? null : customer.Name.Trim();

		if (customer.Number is null)
			return null;

		if (customer.Name is null)
			throw new InvalidOperationException("Please enter a name for the new customer or clear the customer field.");

		if (!Helper.ValidatePhoneNumber(customer.Number))
			throw new InvalidOperationException("Please enter a valid phone number for the new customer.");

		return await CustomerData.InsertCustomer(customer, sqlDataAccessTransaction);
	}

	private static async Task<BillModel> ValidateTransaction(BillModel bill, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		bill.Remarks = string.IsNullOrWhiteSpace(bill.Remarks) ? null : bill.Remarks.Trim();

		if (bill.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (bill.LocationId <= 0)
			throw new InvalidOperationException("Please select a location for the transaction.");

		if (bill.DiningTableId <= 0)
			throw new InvalidOperationException("Please select a dining table for the transaction.");

		if (bill.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (bill.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (bill.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (bill.Cash < 0 || bill.Card < 0 || bill.Credit < 0 || bill.UPI < 0)
			throw new InvalidOperationException("Payment amounts cannot be negative.");

		if (!bill.Running && bill.Cash + bill.Card + bill.UPI + bill.Credit != bill.TotalAmount)
			throw new InvalidOperationException("For settled bills, the total payment must equal the total amount.");

		var diningTable = await CommonData.LoadTableDataById<DiningTableModel>(RestaurantNames.DiningTable, bill.DiningTableId, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The selected dining table does not exist.");

		var diningArea = await CommonData.LoadTableDataById<DiningAreaModel>(RestaurantNames.DiningArea, diningTable.DiningAreaId, sqlDataAccessTransaction);
		if (diningArea.LocationId != bill.LocationId)
			throw new InvalidOperationException("The selected dining table does not belong to the selected location.");

		var userId = update ? bill.LastModifiedBy : bill.CreatedBy;
		var billUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);

		if (billUser.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			if (bill.CompanyId != int.Parse(mainCompanyId.Value))
				throw new InvalidOperationException("You can only create transactions for the primary company.");

			if (bill.LocationId != billUser.LocationId)
				throw new InvalidOperationException("You can only create transactions for your assigned location.");

			bill.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		if (!billUser.ChangeProductFinancial)
		{
			if (bill.ItemDiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			if (bill.ServiceChargePercent != 0 || bill.ServiceChargeAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply service charge.");

			if (bill.DiscountPercent != 0 || bill.DiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply discount.");
		}

		if (update)
		{
			var existingBill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, bill.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			if (existingBill.FinancialAccountingId is not null)
				throw new InvalidOperationException("Cannot update a bill with financial accounting.");

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, bill.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!existingBill.Running && !(user.Admin && user.LocationId == 1))
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			await ValidateDayBillsAccountPosting(existingBill.TransactionDateTime, existingBill.LocationId, sqlDataAccessTransaction);
			await FinancialYearData.ValidateFinancialYear(existingBill.TransactionDateTime, sqlDataAccessTransaction);

			bill.TransactionNo = existingBill.TransactionNo;
		}
		else
			bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(bill, sqlDataAccessTransaction);

		await ValidateDayBillsAccountPosting(bill.TransactionDateTime, bill.LocationId, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime, sqlDataAccessTransaction);

		return bill;
	}

	private static async Task ValidateItemDetails(BillModel bill, List<BillDetailModel> billDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		// On settlement (fully paid) the KOT has already been printed, so clear the flags.
		if (!bill.Running)
			foreach (var item in billDetails)
				item.KOTPrint = false;

		foreach (var item in billDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (billDetails is null || billDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (billDetails.Any(ed => ed.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (billDetails.Any(ed => !ed.Status))
			throw new InvalidOperationException("Bill detail items must be active.");

		if (billDetails.Count != bill.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (billDetails.Sum(ed => ed.Quantity) != bill.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (billDetails.Sum(ed => ed.BaseTotal) != bill.BaseTotal)
			throw new InvalidOperationException("Base total must be equal to the sum of item base totals.");

		if (billDetails.Sum(ed => ed.DiscountAmount) != bill.ItemDiscountAmount)
			throw new InvalidOperationException("Item discount amount must be equal to the sum of item discount amounts.");

		if (billDetails.Sum(ed => ed.AfterDiscount) != bill.TotalAfterItemDiscount)
			throw new InvalidOperationException("Total after item discount must be equal to the sum of item totals after discount.");

		if (billDetails.Sum(ed => ed.TotalTaxAmount) != bill.TotalInclusiveTaxAmount + bill.TotalExtraTaxAmount)
			throw new InvalidOperationException("Total tax amount must be equal to the sum of inclusive and extra tax amounts.");

		if (billDetails.Sum(ed => ed.Total) != bill.TotalAfterTax)
			throw new InvalidOperationException("Total after tax must be equal to the sum of item totals.");

		if (bill.LocationId != 1 && billDetails.Any(ed => ed.IGSTAmount != 0 || ed.IGSTPercent != 0))
			throw new InvalidOperationException("IGST cannot be applied for non-primary location transactions.");

		var userId = update ? bill.LastModifiedBy : bill.CreatedBy;
		var billUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);
		if (!billUser.ChangeProductFinancial)
		{
			if (billDetails.Any(ed => ed.DiscountAmount != 0 || ed.DiscountPercent != 0))
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: bill.LocationId, sqlDataAccessTransaction: sqlDataAccessTransaction);
			var taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax, sqlDataAccessTransaction);

			foreach (var item in billDetails)
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
		BillModel bill,
		List<BillDetailModel> billDetails,
		CustomerModel customer = null,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = bill.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover && !bill.Running
				? await BillInvoiceExport.ExportInvoice(bill.Id, InvoiceExportType.PDF) : null;

			bill.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(bill, billDetails, customer, recover, transaction));

			if (!recover && !bill.Running)
				await BillNotify.Notify(bill.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return bill.Id;
		}

		if (!recover && customer is not null)
			bill.CustomerId = await ResolveCustomer(customer, sqlDataAccessTransaction);

		bill = await ValidateTransaction(bill, update, sqlDataAccessTransaction);
		await ValidateItemDetails(bill, billDetails, update, sqlDataAccessTransaction);

		var previousBill = update ? await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, bill.Id, sqlDataAccessTransaction) : new();
		bool previousRunning = !update || previousBill is null || previousBill.Running;
		// Audit edits only of already-settled bills; the first settlement of a running draft is recorded as an Insert.
		bool auditUpdate = update && !previousRunning;
		var previousBillDetails = auditUpdate && !recover && !bill.Running ? await CommonData.LoadTableDataByMasterId<BillItemOverviewModel>(RestaurantNames.BillItemOverview, bill.Id, sqlDataAccessTransaction) : [];

		bill.Id = await InsertBill(bill, sqlDataAccessTransaction);
		await SaveTransactionDetail(bill, billDetails, update, previousRunning, sqlDataAccessTransaction);

		if (!bill.Running)
		{
			await SaveProductStock(bill, billDetails, sqlDataAccessTransaction);
			await SaveRawMaterialStockByRecipe(bill, billDetails, sqlDataAccessTransaction);
			await SaveAccounting(bill, sqlDataAccessTransaction);
			await SaveAuditTrail(bill, auditUpdate, recover, previousBill, previousBillDetails, sqlDataAccessTransaction);
		}

		return bill.Id;
	}

	private static async Task SaveTransactionDetail(BillModel bill, List<BillDetailModel> billDetails, bool update, bool previousRunning, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingBillDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(RestaurantNames.BillDetail, bill.Id, sqlDataAccessTransaction);
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
			await InsertBillDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(BillModel bill, List<BillDetailModel> billDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(bill.TransactionNo, sqlDataAccessTransaction);

		foreach (var item in billDetails)
			await ProductStockData.InsertProductStock(new()
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
	}

	private static async Task SaveRawMaterialStockByRecipe(BillModel bill, List<BillDetailModel> billDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(bill.TransactionNo, sqlDataAccessTransaction);

		if (bill.LocationId != 1)
			return;

		var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(InventoryNames.Recipe, true, sqlDataAccessTransaction);
		recipes = [.. recipes.Where(r => r.Deduct)];
		var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(InventoryNames.RecipeDetail, true, sqlDataAccessTransaction);

		foreach (var product in billDetails)
		{
			var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
			var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = -recipeItem.Quantity * (product.Quantity / recipe.Quantity),
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = bill.Id,
					TransactionNo = bill.TransactionNo,
					Type = nameof(StockType.Bill),
					TransactionDateTime = bill.TransactionDateTime
				}, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAccounting(BillModel bill, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await DeleteAccounting(bill, sqlDataAccessTransaction);

		if (bill.LocationId != 1)
			return;

		var billOverview = await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, bill.Id, sqlDataAccessTransaction);
		if (billOverview is null || billOverview.TotalAmount == 0)
			return;

		var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
		var billLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.BillLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit > 0)
			accountingCart.Add(new()
			{
				ReferenceId = billOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Bill),
				ReferenceNo = billOverview.TransactionNo,
				LedgerId = int.Parse(cashLedger.Value),
				Debit = billOverview.Cash + billOverview.UPI + billOverview.Card + billOverview.Credit,
				Credit = null,
				Remarks = $"Cash Account Posting For Bill {billOverview.TransactionNo}",
			});

		if (billOverview.TotalAmount - billOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = billOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Bill),
				ReferenceNo = billOverview.TransactionNo,
				LedgerId = int.Parse(billLedger.Value),
				Debit = null,
				Credit = billOverview.TotalAmount - billOverview.TotalExtraTaxAmount,
				Remarks = $"Bill Account Posting For Bill {billOverview.TransactionNo}",
			});

		if (billOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = billOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Bill),
				ReferenceNo = billOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = billOverview.TotalExtraTaxAmount,
				Remarks = $"GST Account Posting For Bill {billOverview.TransactionNo}",
			});

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

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
		await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		bill.FinancialAccountingId = accounting.Id;
		await InsertBill(bill, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		BillModel bill,
		bool update,
		bool recover,
		BillOverviewModel previousBill = null,
		List<BillItemOverviewModel> previousBillDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentBill = await CommonData.LoadTableDataById<BillOverviewModel>(RestaurantNames.BillOverview, bill.Id, sqlDataAccessTransaction);
			var currentBillDetails = await CommonData.LoadTableDataByMasterId<BillItemOverviewModel>(RestaurantNames.BillItemOverview, bill.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousBill, currentBill);
			var detailsDiff = AuditTrailData.GetDifference(previousBillDetails, currentBillDetails, typeof(BillOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = RestaurantNames.Bill,
			RecordNo = bill.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? bill.LastModifiedBy.Value : bill.CreatedBy,
			CreatedFromPlatform = update ? bill.LastModifiedFromPlatform : bill.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	#region Posting
	private static async Task ValidateDayBillsAccountPosting(DateTime postDate, int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var bills = await CommonData.LoadTableDataByDate<BillModel>(
			RestaurantNames.Bill,
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MinValue),
			sqlDataAccessTransaction);

		if (bills.Any(b => b.LocationId == locationId && b.FinancialAccountingId is not null && b.Status))
			throw new InvalidOperationException("Cannot post day bills account entry as there are already posted bills for the day. Please contact administrator.");
	}

	public static async Task PostDayBills(DateTime postingDate, int locationId, int userId, string userPlatform)
	{
		await ValidateDayBillsAccountPosting(postingDate, locationId);
		await FinancialYearData.ValidateFinancialYear(postingDate);

		var bills = await CommonData.LoadTableDataByDate<BillModel>(
				RestaurantNames.Bill,
				DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MinValue));

		bills = [.. bills.Where(b =>
									b.LocationId == locationId &&
									b.FinancialAccountingId is null &&
									b.Status &&
									!b.Running)];

		if (bills.Count == 0)
			return;

		if (bills.Any(s => s.LocationId == 1))
			throw new InvalidOperationException("Cannot post day bills account entry for the primary location. Please contact administrator.");

		var totalAmount = bills.Sum(b => b.Cash + b.UPI + b.Card + b.Credit);
		var totalExtraTaxAmount = bills.Sum(b => b.TotalExtraTaxAmount);

		var ledger = await LocationData.LoadLedgerByLocationId(locationId);
		var billLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.BillLedgerId);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (totalAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = ledger.Id,
				Debit = totalAmount,
				Credit = null,
				Remarks = "Location Account Posting For Bill Day Closing",
			});

		if (totalAmount - totalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(billLedger.Value),
				Debit = null,
				Credit = totalAmount - totalExtraTaxAmount,
				Remarks = "Bill Account Posting For Bill Day Closing",
			});

		if (totalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = totalExtraTaxAmount,
				Remarks = "GST Account Posting For Bill Day Closing",
			});

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

		await SqlDataAccessTransaction.Run(async sqlDataAccessTransaction =>
		{
			var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
			accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

			foreach (var billOverview in bills)
			{
				var bill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, billOverview.Id, sqlDataAccessTransaction);
				bill.FinancialAccountingId = accounting.Id;
				await InsertBill(bill, sqlDataAccessTransaction);
			}
		});

		await BillNotify.NotifyDayClosing(
			postingDate,
			locationId,
			bills.Count,
			totalAmount,
			totalExtraTaxAmount,
			userId,
			accounting.TransactionNo);
	}
	#endregion
}
