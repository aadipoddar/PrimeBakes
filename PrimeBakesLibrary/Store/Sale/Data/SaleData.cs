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
using PrimeBakesLibrary.Store.Customer;
using PrimeBakesLibrary.Store.Order.Data;
using PrimeBakesLibrary.Store.Order.Models;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Store.Sale.Data;

public static class SaleData
{
	private static async Task<int> InsertSale(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertSale, sale, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Sale.");

	private static async Task<int> InsertSaleDetail(SaleDetailModel saleDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertSaleDetail, saleDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Sale Detail.");

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var sales = await CommonData.LoadTableDataByFinancialAccountingId<SaleModel>(StoreNames.Sale, financialAccountingId, sqlDataAccessTransaction);
		foreach (var sale in sales)
		{
			sale.FinancialAccountingId = newFinancialAccountingId;
			await InsertSale(sale, sqlDataAccessTransaction);
		}
	}

	public static async Task ApplyItemFinancialDetails(List<SaleItemCartModel> cart, List<ProductModel> products, List<TaxModel> taxes)
	{
		foreach (var item in cart.Where(i => i.Quantity > 0))
		{
			item.DiscountPercent = 0;
			item.DiscountAmount = 0;

			item.BaseTotal = item.Rate * item.Quantity;
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

			var product = products.FirstOrDefault(p => p.Id == item.ItemId);
			var tax = taxes.FirstOrDefault(t => t.Id == product?.TaxId);

			item.CGSTPercent = tax?.CGST ?? 0;
			item.SGSTPercent = tax?.SGST ?? 0;
			item.IGSTPercent = 0;
			item.InclusiveTax = tax?.Inclusive ?? false;

			if (item.InclusiveTax)
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount;
			}
			else
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount + item.TotalTaxAmount;
			}

			item.NetRate = item.Total / item.Quantity;
			item.Remarks = null;
		}
	}

	public static List<SaleDetailModel> ConvertCartToDetails(List<SaleItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new SaleDetailModel
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
	public static async Task DeleteTransaction(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(sale, transaction));
			await SaleNotify.Notify(sale.Id, NotifyType.Deleted);
			return;
		}

		await ValidateDaySalesAccountPosting(sale.TransactionDateTime, sale.LocationId, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(sale.TransactionDateTime, sqlDataAccessTransaction);

		if (sale.OrderId is not null && sale.OrderId > 0)
			await OrderData.LinkOrderToSale(sale.OrderId, sale.Id, true, sqlDataAccessTransaction);

		sale.OrderId = null;
		sale.Status = false;
		await InsertSale(sale, sqlDataAccessTransaction);

		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(sale.TransactionNo, sqlDataAccessTransaction);
		await ProductStockData.DeleteProductStockByTransactionNo(sale.TransactionNo, sqlDataAccessTransaction);
		await DeleteAccounting(sale, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = StoreNames.Sale,
			RecordNo = sale.TransactionNo,
			CreatedBy = sale.LastModifiedBy.Value,
			CreatedFromPlatform = sale.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (sale.FinancialAccountingId is null)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, sale.FinancialAccountingId ?? 0, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = sale.LastModifiedBy;
		existingAccounting.LastModifiedAt = sale.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(SaleModel sale)
	{
		sale.Status = true;
		var saleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(StoreNames.SaleDetail, sale.Id);
		await SaveTransaction(sale, saleDetails, null, true);

		await SaleNotify.Notify(sale.Id, NotifyType.Recovered);
	}

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

	private static async Task<SaleModel> ValidateTransaction(SaleModel sale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		sale.Remarks = string.IsNullOrWhiteSpace(sale.Remarks) ? null : sale.Remarks.Trim();

		if (sale.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (sale.LocationId <= 0)
			throw new InvalidOperationException("Please select a location for the transaction.");

		if (sale.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (sale.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (sale.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (sale.Cash < 0 || sale.Card < 0 || sale.Credit < 0 || sale.UPI < 0)
			throw new InvalidOperationException("Payment amounts cannot be negative.");

		if (sale.Cash + sale.Card + sale.Credit + sale.UPI != sale.TotalAmount)
			throw new InvalidOperationException("The sum of all payments must equal the total amount of the transaction.");

		if (sale.Credit > 0 && (sale.PartyId is null || sale.PartyId <= 0))
			throw new InvalidOperationException("Please select a party ledger for credit payment.");

		var userId = update ? sale.LastModifiedBy : sale.CreatedBy;
		var saleUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);

		if (saleUser.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			if (sale.CompanyId != int.Parse(mainCompanyId.Value))
				throw new InvalidOperationException("You can only create transactions for the primary company.");

			if (sale.LocationId != saleUser.LocationId)
				throw new InvalidOperationException("You can only create transactions for your assigned location.");

			if (sale.ItemDiscountAmount != 0)
				throw new InvalidOperationException("Item discount cannot be applied for non-primary location transactions.");

			if (sale.OtherChargesPercent != 0 || sale.OtherChargesAmount != 0)
				throw new InvalidOperationException("Other charges cannot be applied for non-primary location transactions.");

			if (sale.DiscountPercent != 0 || sale.DiscountAmount != 0)
				throw new InvalidOperationException("Discount amount cannot be applied for non-primary location transactions.");

			sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		if (sale.LocationId != 1)
		{
			if (sale.PartyId is not null)
				throw new InvalidOperationException("Party cannot be selected for non-primary location transactions.");

			if (sale.OrderId is not null)
				throw new InvalidOperationException("Order cannot be linked for non-primary location transactions.");

			if (sale.Credit != 0)
				throw new InvalidOperationException("Credit payment is not allowed for non-primary location transactions.");
		}

		if (sale.OrderId is not null && sale.OrderId > 0)
		{
			if (sale.PartyId is null || sale.PartyId <= 0)
				throw new InvalidOperationException("An order can only be linked when a party is selected.");

			var partyLocation = await LocationData.LoadLocationByLedgerId(sale.PartyId.Value, sqlDataAccessTransaction);
			var order = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, sale.OrderId.Value, sqlDataAccessTransaction);
			if (order is null || !order.Status)
				throw new InvalidOperationException("The selected order is invalid or does not exist.");

			if ((sale.Id == 0 && order.SaleId is not null) || order.LocationId != partyLocation.Id)
				throw new InvalidOperationException("The selected order is invalid or does not belong to the selected party's location.");
		}

		if (update)
		{
			var existingSale = await CommonData.LoadTableDataById<SaleModel>(StoreNames.Sale, sale.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The sale transaction does not exist.");

			await ValidateDaySalesAccountPosting(existingSale.TransactionDateTime, existingSale.LocationId, sqlDataAccessTransaction);
			await FinancialYearData.ValidateFinancialYear(existingSale.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, sale.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users can update a sale transaction.");

			sale.TransactionNo = existingSale.TransactionNo;
		}
		else
			sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(sale, sqlDataAccessTransaction);

		await ValidateDaySalesAccountPosting(sale.TransactionDateTime, sale.LocationId, sqlDataAccessTransaction);
		await FinancialYearData.ValidateFinancialYear(sale.TransactionDateTime, sqlDataAccessTransaction);

		return sale;
	}

	private static async Task ValidateItemDetails(SaleModel sale, List<SaleDetailModel> saleDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		foreach (var item in saleDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (saleDetails is null || saleDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (saleDetails.Any(ed => ed.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (saleDetails.Any(ed => !ed.Status))
			throw new InvalidOperationException("Sale detail items must be active.");

		if (saleDetails.Count != sale.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (saleDetails.Sum(ed => ed.Quantity) != sale.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (saleDetails.Sum(ed => ed.BaseTotal) != sale.BaseTotal)
			throw new InvalidOperationException("Base total must be equal to the sum of item base totals.");

		if (saleDetails.Sum(ed => ed.DiscountAmount) != sale.ItemDiscountAmount)
			throw new InvalidOperationException("Item discount amount must be equal to the sum of item discount amounts.");

		if (saleDetails.Sum(ed => ed.AfterDiscount) != sale.TotalAfterItemDiscount)
			throw new InvalidOperationException("Total after item discount must be equal to the sum of item totals after discount.");

		if (saleDetails.Sum(ed => ed.TotalTaxAmount) != sale.TotalInclusiveTaxAmount + sale.TotalExtraTaxAmount)
			throw new InvalidOperationException("Total tax amount must be equal to the sum of inclusive and extra tax amounts.");

		if (saleDetails.Sum(ed => ed.Total) != sale.TotalAfterTax)
			throw new InvalidOperationException("Total after tax must be equal to the sum of item totals.");

		if (sale.LocationId != 1)
		{
			if (saleDetails.Any(ed => ed.IGSTAmount != 0 || ed.IGSTPercent != 0))
				throw new InvalidOperationException("IGST cannot be applied for non-primary location transactions.");
		}

		var userId = update ? sale.LastModifiedBy : sale.CreatedBy;
		var saleUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);
		if (saleUser.LocationId != 1)
		{
			if (saleDetails.Any(ed => ed.DiscountAmount != 0 || ed.DiscountPercent != 0))
				throw new InvalidOperationException("Item discount cannot be applied for non-primary location transactions.");

			foreach (var item in saleDetails)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId, sqlDataAccessTransaction);
				var tax = await CommonData.LoadTableDataById<TaxModel>(StoreNames.Tax, product.TaxId, sqlDataAccessTransaction);

				if (item.Rate != product.Rate)
					throw new InvalidOperationException($"Item rate for product '{product.Name}' cannot be changed for non-primary location transactions.");

				if (item.CGSTPercent != tax.CGST || item.SGSTPercent != tax.SGST)
					throw new InvalidOperationException($"Item tax for product '{product.Name}' cannot be changed for non-primary location transactions.");

				if (item.InclusiveTax != tax.Inclusive)
					throw new InvalidOperationException($"Item tax inclusion for product '{product.Name}' cannot be changed for non-primary location transactions.");
			}
		}
	}

	public static async Task<int> SaveTransaction(
		SaleModel sale,
		List<SaleDetailModel> saleDetails,
		CustomerModel customer = null,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = sale.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await SaleInvoiceExport.ExportInvoice(sale.Id, InvoiceExportType.PDF) : null;

			sale.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(sale, saleDetails, customer, recover, transaction));

			if (!recover)
				await SaleNotify.Notify(sale.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return sale.Id;
		}

		if (!recover && customer is not null)
			sale.CustomerId = await ResolveCustomer(customer, sqlDataAccessTransaction);

		sale = await ValidateTransaction(sale, update, sqlDataAccessTransaction);
		await ValidateItemDetails(sale, saleDetails, update, sqlDataAccessTransaction);

		var previousSale = update && !recover ? await CommonData.LoadTableDataById<SaleOverviewModel>(StoreNames.SaleOverview, sale.Id, sqlDataAccessTransaction) : new();
		var previousSaleDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<SaleItemOverviewModel>(StoreNames.SaleItemOverview, sale.Id, sqlDataAccessTransaction) : [];

		sale.Id = await InsertSale(sale, sqlDataAccessTransaction);
		await SaveTransactionDetail(sale, saleDetails, update, sqlDataAccessTransaction);
		await SaveProductStock(sale, saleDetails, sqlDataAccessTransaction);
		await SaveRawMaterialStockByRecipe(sale, saleDetails, sqlDataAccessTransaction);
		await UpdateOrder(sale, previousSale, update, sqlDataAccessTransaction);
		await SaveAccounting(sale, update, sqlDataAccessTransaction);
		await SaveAuditTrail(sale, update, recover, previousSale, previousSaleDetails, sqlDataAccessTransaction);

		return sale.Id;
	}

	private static async Task SaveTransactionDetail(SaleModel sale, List<SaleDetailModel> saleDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingSaleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(StoreNames.SaleDetail, sale.Id, sqlDataAccessTransaction);
			foreach (var item in existingSaleDetails)
			{
				item.Status = false;
				await InsertSaleDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in saleDetails)
		{
			item.MasterId = sale.Id;
			await InsertSaleDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(SaleModel sale, List<SaleDetailModel> saleDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(sale.TransactionNo, sqlDataAccessTransaction);

		foreach (var item in saleDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = sale.Id,
				Type = nameof(StockType.Sale),
				TransactionNo = sale.TransactionNo,
				TransactionDateTime = sale.TransactionDateTime,
				LocationId = sale.LocationId
			}, sqlDataAccessTransaction);

		if (sale.PartyId is not null and > 0)
		{
			var location = await LocationData.LoadLocationByLedgerId(sale.PartyId.Value, sqlDataAccessTransaction);
			if (location is not null)
				foreach (var item in saleDetails)
					await ProductStockData.InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ProductId,
						Quantity = item.Quantity,
						NetRate = item.NetRate,
						TransactionId = sale.Id,
						Type = nameof(StockType.Purchase),
						TransactionNo = sale.TransactionNo,
						TransactionDateTime = sale.TransactionDateTime,
						LocationId = location.Id
					}, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStockByRecipe(SaleModel sale, List<SaleDetailModel> saleDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(sale.TransactionNo, sqlDataAccessTransaction);

		if (sale.LocationId != 1)
			return;

		var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(InventoryNames.Recipe, true, sqlDataAccessTransaction);
		recipes = [.. recipes.Where(r => r.Deduct)];
		var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(InventoryNames.RecipeDetail, true, sqlDataAccessTransaction);

		foreach (var product in saleDetails)
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
					TransactionId = sale.Id,
					TransactionNo = sale.TransactionNo,
					Type = nameof(StockType.Sale),
					TransactionDateTime = sale.TransactionDateTime
				}, sqlDataAccessTransaction);
		}
	}

	private static async Task UpdateOrder(SaleModel sale, SaleOverviewModel previousSale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
			await OrderData.LinkOrderToSale(previousSale.OrderId, previousSale.Id, true, sqlDataAccessTransaction);

		if (sale.OrderId is not null)
			await OrderData.LinkOrderToSale(sale.OrderId, sale.Id, false, sqlDataAccessTransaction);
	}

	private static async Task SaveAccounting(SaleModel sale, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId, sqlDataAccessTransaction);
			var existingAccounting = await FinancialAccountingData.LoadFinancialAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo, sqlDataAccessTransaction);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				existingAccounting.LastModifiedBy = sale.LastModifiedBy;
				existingAccounting.LastModifiedAt = sale.LastModifiedAt;
				existingAccounting.LastModifiedFromPlatform = sale.LastModifiedFromPlatform;

				await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
			}
		}

		if (sale.LocationId != 1)
			return;

		var saleOverview = await CommonData.LoadTableDataById<SaleOverviewModel>(StoreNames.SaleOverview, sale.Id, sqlDataAccessTransaction);
		if (saleOverview is null || saleOverview.TotalAmount == 0)
			return;

		var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
		var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (saleOverview.Cash + saleOverview.UPI + saleOverview.Card > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Sale),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(cashLedger.Value),
				Debit = saleOverview.Cash + saleOverview.UPI + saleOverview.Card,
				Credit = null,
				Remarks = $"Cash Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});

		if (saleOverview.Credit > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Sale),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = saleOverview.PartyId.Value,
				Debit = saleOverview.Credit,
				Credit = null,
				Remarks = $"Party Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});

		if (saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Sale),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(saleLedger.Value),
				Debit = null,
				Credit = saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount,
				Remarks = $"Sale Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});

		if (saleOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.Sale),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = saleOverview.TotalExtraTaxAmount,
				Remarks = $"GST Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = saleOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = saleOverview.Id,
			ReferenceNo = saleOverview.TransactionNo,
			TransactionDateTime = saleOverview.TransactionDateTime,
			FinancialYearId = saleOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = saleOverview.Remarks,
			CreatedBy = saleOverview.CreatedBy,
			CreatedAt = saleOverview.CreatedAt,
			CreatedFromPlatform = saleOverview.CreatedFromPlatform,
			Status = true
		};

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
		await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		sale.FinancialAccountingId = accounting.Id;
		await InsertSale(sale, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		SaleModel sale,
		bool update,
		bool recover,
		SaleOverviewModel previousSale = null,
		List<SaleItemOverviewModel> previousSaleDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentSale = await CommonData.LoadTableDataById<SaleOverviewModel>(StoreNames.SaleOverview, sale.Id, sqlDataAccessTransaction);
			var currentSaleDetails = await CommonData.LoadTableDataByMasterId<SaleItemOverviewModel>(StoreNames.SaleItemOverview, sale.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousSale, currentSale);
			var detailsDiff = AuditTrailData.GetDifference(previousSaleDetails, currentSaleDetails, typeof(SaleOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = StoreNames.Sale,
			RecordNo = sale.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? sale.LastModifiedBy.Value : sale.CreatedBy,
			CreatedFromPlatform = update ? sale.LastModifiedFromPlatform : sale.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	#region Posting
	private static async Task ValidateDaySalesAccountPosting(DateTime postDate, int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (locationId == 1)
			return;

		var sales = await CommonData.LoadTableDataByDate<SaleModel>(
			StoreNames.Sale,
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(postDate).ToDateTime(TimeOnly.MinValue),
			sqlDataAccessTransaction);

		if (sales.Any(s => s.LocationId == locationId && s.FinancialAccountingId is not null && s.Status))
			throw new InvalidOperationException("Cannot post day sales account entry as there are already posted sales for the day. Please contact administrator.");
	}

	public static async Task PostDaySales(DateTime postingDate, int locationId, int userId, string userPlatform)
	{
		await ValidateDaySalesAccountPosting(postingDate, locationId);
		await FinancialYearData.ValidateFinancialYear(postingDate);

		var sales = await CommonData.LoadTableDataByDate<SaleModel>(
			StoreNames.Sale,
			DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(postingDate).ToDateTime(TimeOnly.MinValue));

		sales = [.. sales.Where(s =>
									s.LocationId == locationId &&
									s.FinancialAccountingId is null &&
									s.Status)];

		if (sales.Count == 0)
			return;

		if (sales.Any(s => s.LocationId == 1))
			throw new InvalidOperationException("Cannot post day sales account entry for the primary location. Please contact administrator.");

		var totalAmount = sales.Sum(s => s.Cash + s.UPI + s.Card + s.Credit);
		var totalExtraTaxAmount = sales.Sum(s => s.TotalExtraTaxAmount);

		var ledger = await LocationData.LoadLedgerByLocationId(locationId);
		var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (totalAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = ledger.Id,
				Debit = totalAmount,
				Credit = null,
				Remarks = "Location Account Posting For Sale Day Closing",
			});

		if (totalAmount - totalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(saleLedger.Value),
				Debit = null,
				Credit = totalAmount - totalExtraTaxAmount,
				Remarks = "Sale Account Posting For Sale Day Closing",
			});

		if (totalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = totalExtraTaxAmount,
				Remarks = "GST Account Posting For Sale Day Closing",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleDayCloseVoucherId);
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = sales.First().CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = null,
			ReferenceNo = null,
			TransactionDateTime = DateOnly.FromDateTime(postingDate).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second)),
			FinancialYearId = financialYear.Id,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = $"Sale Day Closing for {postingDate:dd-MMM-yyyy}",
			CreatedBy = userId,
			CreatedAt = currentDateTime,
			CreatedFromPlatform = userPlatform,
			Status = true
		};

		await SqlDataAccessTransaction.Run(async sqlDataAccessTransaction =>
		{
			var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
			accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

			foreach (var sale in sales)
			{
				sale.FinancialAccountingId = accounting.Id;
				await InsertSale(sale, sqlDataAccessTransaction);
			}
		});

		await SaleNotify.NotifyDayClosing(
			postingDate,
			locationId,
			sales.Count,
			totalAmount,
			totalExtraTaxAmount,
			userId,
			accounting.TransactionNo);
	}
	#endregion
}
