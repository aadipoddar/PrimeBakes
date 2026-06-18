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
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Store.Sale.Data;

public static class SaleReturnData
{
	private static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertSaleReturn, saleReturn, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Sale Return.");

	private static async Task<int> InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertSaleReturnDetail, saleReturnDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Sale Return Detail.");

	internal static async Task UpdateFinancialAccountingId(int financialAccountingId, int? newFinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var saleReturns = await CommonData.LoadTableDataByFinancialAccountingId<SaleReturnModel>(StoreNames.SaleReturn, financialAccountingId, sqlDataAccessTransaction);
		foreach (var saleReturn in saleReturns)
		{
			saleReturn.FinancialAccountingId = newFinancialAccountingId;
			await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);
		}
	}

	public static List<SaleReturnDetailModel> ConvertCartToDetails(List<SaleReturnItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new SaleReturnDetailModel
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
	public static async Task DeleteTransaction(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(saleReturn, transaction));
			await SaleReturnNotify.Notify(saleReturn.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(saleReturn.TransactionDateTime, sqlDataAccessTransaction);

		saleReturn.Status = false;
		await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);

		await ProductStockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(saleReturn.TransactionNo, sqlDataAccessTransaction);
		await DeleteAccounting(saleReturn, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = StoreNames.SaleReturn,
			RecordNo = saleReturn.TransactionNo,
			CreatedBy = saleReturn.LastModifiedBy.Value,
			CreatedFromPlatform = saleReturn.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	private static async Task DeleteAccounting(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (saleReturn.FinancialAccountingId is null || saleReturn.FinancialAccountingId <= 0)
			return;

		var existingAccounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, saleReturn.FinancialAccountingId.Value, sqlDataAccessTransaction)
			?? throw new InvalidOperationException("The associated financial accounting transaction for the transaction does not exist.");

		existingAccounting.Status = false;
		existingAccounting.LastModifiedBy = saleReturn.LastModifiedBy;
		existingAccounting.LastModifiedAt = saleReturn.LastModifiedAt;
		existingAccounting.LastModifiedFromPlatform = saleReturn.LastModifiedFromPlatform;

		await FinancialAccountingData.DeleteTransaction(existingAccounting, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(SaleReturnModel saleReturn)
	{
		saleReturn.Status = true;
		var saleReturnDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(StoreNames.SaleReturnDetail, saleReturn.Id);
		await SaveTransaction(saleReturn, saleReturnDetails, null, true);

		await SaleReturnNotify.Notify(saleReturn.Id, NotifyType.Recovered);
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

	private static async Task<SaleReturnModel> ValidateTransaction(SaleReturnModel saleReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		saleReturn.Remarks = string.IsNullOrWhiteSpace(saleReturn.Remarks) ? null : saleReturn.Remarks.Trim();

		if (saleReturn.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (saleReturn.LocationId <= 0)
			throw new InvalidOperationException("Please select a location for the transaction.");

		if (saleReturn.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (saleReturn.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (saleReturn.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (saleReturn.Cash < 0 || saleReturn.Card < 0 || saleReturn.Credit < 0 || saleReturn.UPI < 0)
			throw new InvalidOperationException("Payment amounts cannot be negative.");

		if (saleReturn.Cash + saleReturn.Card + saleReturn.Credit + saleReturn.UPI != saleReturn.TotalAmount)
			throw new InvalidOperationException("The sum of all payments must equal the total amount of the transaction.");

		if (saleReturn.Credit > 0 && (saleReturn.PartyId is null || saleReturn.PartyId <= 0))
			throw new InvalidOperationException("Please select a party ledger for credit payment.");

		var userId = update ? saleReturn.LastModifiedBy : saleReturn.CreatedBy;
		var saleReturnUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);

		if (saleReturnUser.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			if (saleReturn.CompanyId != int.Parse(mainCompanyId.Value))
				throw new InvalidOperationException("You can only create transactions for the primary company.");

			if (saleReturn.LocationId != saleReturnUser.LocationId)
				throw new InvalidOperationException("You can only create transactions for your assigned location.");

			saleReturn.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		if (!saleReturnUser.ChangeProductFinancial)
		{
			if (saleReturn.ItemDiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			if (saleReturn.OtherChargesPercent != 0 || saleReturn.OtherChargesAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply other charges.");

			if (saleReturn.DiscountPercent != 0 || saleReturn.DiscountAmount != 0)
				throw new InvalidOperationException("You are not allowed to apply discount.");
		}

		if (saleReturn.LocationId != 1)
		{
			if (saleReturn.PartyId is not null)
				throw new InvalidOperationException("Party cannot be selected for non-primary location transactions.");

			if (saleReturn.Credit != 0)
				throw new InvalidOperationException("Credit payment is not allowed for non-primary location transactions.");
		}

		if (update)
		{
			var existingSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(StoreNames.SaleReturn, saleReturn.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingSaleReturn.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, saleReturn.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			saleReturn.TransactionNo = existingSaleReturn.TransactionNo;
		}
		else
			saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(saleReturn.TransactionDateTime, sqlDataAccessTransaction);

		return saleReturn;
	}

	private static async Task ValidateItemDetails(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		foreach (var item in saleReturnDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (saleReturnDetails is null || saleReturnDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (saleReturnDetails.Any(ed => ed.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (saleReturnDetails.Any(ed => !ed.Status))
			throw new InvalidOperationException("Sale return detail items must be active.");

		if (saleReturnDetails.Count != saleReturn.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (saleReturnDetails.Sum(ed => ed.Quantity) != saleReturn.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (saleReturnDetails.Sum(ed => ed.BaseTotal) != saleReturn.BaseTotal)
			throw new InvalidOperationException("Base total must be equal to the sum of item base totals.");

		if (saleReturnDetails.Sum(ed => ed.DiscountAmount) != saleReturn.ItemDiscountAmount)
			throw new InvalidOperationException("Item discount amount must be equal to the sum of item discount amounts.");

		if (saleReturnDetails.Sum(ed => ed.AfterDiscount) != saleReturn.TotalAfterItemDiscount)
			throw new InvalidOperationException("Total after item discount must be equal to the sum of item totals after discount.");

		if (saleReturnDetails.Sum(ed => ed.TotalTaxAmount) != saleReturn.TotalInclusiveTaxAmount + saleReturn.TotalExtraTaxAmount)
			throw new InvalidOperationException("Total tax amount must be equal to the sum of inclusive and extra tax amounts.");

		if (saleReturnDetails.Sum(ed => ed.Total) != saleReturn.TotalAfterTax)
			throw new InvalidOperationException("Total after tax must be equal to the sum of item totals.");

		if (saleReturn.LocationId != 1)
		{
			if (saleReturnDetails.Any(ed => ed.IGSTAmount != 0 || ed.IGSTPercent != 0))
				throw new InvalidOperationException("IGST cannot be applied for non-primary location transactions.");
		}

		var userId = update ? saleReturn.LastModifiedBy : saleReturn.CreatedBy;
		var saleReturnUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, userId.Value, sqlDataAccessTransaction);
		if (!saleReturnUser.ChangeProductFinancial)
		{
			if (saleReturnDetails.Any(ed => ed.DiscountAmount != 0 || ed.DiscountPercent != 0))
				throw new InvalidOperationException("You are not allowed to apply item discount.");

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: saleReturn.LocationId, sqlDataAccessTransaction: sqlDataAccessTransaction);
			var taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax, sqlDataAccessTransaction);

			foreach (var item in saleReturnDetails)
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
		SaleReturnModel saleReturn,
		List<SaleReturnDetailModel> saleReturnDetails,
		CustomerModel customer = null,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = saleReturn.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await SaleReturnInvoiceExport.ExportInvoice(saleReturn.Id, InvoiceExportType.PDF) : null;

			saleReturn.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(saleReturn, saleReturnDetails, customer, recover, transaction));

			if (!recover)
				await SaleReturnNotify.Notify(saleReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return saleReturn.Id;
		}

		if (!recover && customer is not null)
			saleReturn.CustomerId = await ResolveCustomer(customer, sqlDataAccessTransaction);

		saleReturn = await ValidateTransaction(saleReturn, update, sqlDataAccessTransaction);
		await ValidateItemDetails(saleReturn, saleReturnDetails, update, sqlDataAccessTransaction);

		var previousSaleReturn = update && !recover ? await CommonData.LoadTableDataById<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, saleReturn.Id, sqlDataAccessTransaction) : new();
		var previousSaleReturnDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, saleReturn.Id, sqlDataAccessTransaction) : [];

		saleReturn.Id = await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);
		await SaveTransactionDetail(saleReturn, saleReturnDetails, update, sqlDataAccessTransaction);
		await SaveProductStock(saleReturn, saleReturnDetails, sqlDataAccessTransaction);
		await SaveRawMaterialStockByRecipe(saleReturn, saleReturnDetails, sqlDataAccessTransaction);
		await SaveAccounting(saleReturn, sqlDataAccessTransaction);
		await SaveAuditTrail(saleReturn, update, recover, previousSaleReturn, previousSaleReturnDetails, sqlDataAccessTransaction);

		return saleReturn.Id;
	}

	private static async Task SaveTransactionDetail(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingSaleReturnDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(StoreNames.SaleReturnDetail, saleReturn.Id, sqlDataAccessTransaction);
			foreach (var item in existingSaleReturnDetails)
			{
				item.Status = false;
				await InsertSaleReturnDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in saleReturnDetails)
		{
			item.MasterId = saleReturn.Id;
			await InsertSaleReturnDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo, sqlDataAccessTransaction);

		// Location Stock Update (positive quantity - product returns to location)
		foreach (var item in saleReturnDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				TransactionId = saleReturn.Id,
				Type = nameof(StockType.SaleReturn),
				TransactionNo = saleReturn.TransactionNo,
				TransactionDateTime = saleReturn.TransactionDateTime,
				LocationId = saleReturn.LocationId
			}, sqlDataAccessTransaction);

		// Party Location Stock Update (negative quantity - product leaves party's location)
		if (saleReturn.PartyId is not null and > 0)
		{
			var location = await LocationData.LoadLocationByLedgerId(saleReturn.PartyId.Value, sqlDataAccessTransaction);
			if (location is not null)
				foreach (var item in saleReturnDetails)
					await ProductStockData.InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ProductId,
						Quantity = -item.Quantity,
						NetRate = item.NetRate,
						TransactionId = saleReturn.Id,
						Type = nameof(StockType.PurchaseReturn),
						TransactionNo = saleReturn.TransactionNo,
						TransactionDateTime = saleReturn.TransactionDateTime,
						LocationId = location.Id
					}, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStockByRecipe(SaleReturnModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(saleReturn.TransactionNo, sqlDataAccessTransaction);

		if (saleReturn.LocationId != 1)
			return;

		var recipes = await CommonData.LoadTableDataByStatus<RecipeModel>(InventoryNames.Recipe, true, sqlDataAccessTransaction);
		recipes = [.. recipes.Where(r => r.Deduct)];
		var recipeDetails = await CommonData.LoadTableDataByStatus<RecipeDetailModel>(InventoryNames.RecipeDetail, true, sqlDataAccessTransaction);

		foreach (var product in saleReturnDetails)
		{
			var recipe = recipes.FirstOrDefault(_ => _.ProductId == product.ProductId);
			var recipeItems = recipe is null ? [] : recipeDetails.Where(_ => _.MasterId == recipe.Id).ToList();

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = recipeItem.Quantity * (product.Quantity / recipe.Quantity),
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = saleReturn.Id,
					TransactionNo = saleReturn.TransactionNo,
					Type = nameof(StockType.SaleReturn),
					TransactionDateTime = saleReturn.TransactionDateTime
				}, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAccounting(SaleReturnModel saleReturn, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await DeleteAccounting(saleReturn, sqlDataAccessTransaction);

		var saleReturnOverview = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, saleReturn.Id, sqlDataAccessTransaction);
		if (saleReturnOverview is null || saleReturnOverview.TotalAmount == 0)
			return;

		var ledger = await LocationData.LoadLedgerByLocationId(saleReturnOverview.LocationId, sqlDataAccessTransaction);
		var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId, sqlDataAccessTransaction);
		var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId, sqlDataAccessTransaction);
		var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId, sqlDataAccessTransaction);
		var accountingCart = new List<FinancialAccountingLedgerCartModel>();

		if (saleReturn.LocationId == 1)
		{
			if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card > 0)
				accountingCart.Add(new()
				{
					ReferenceId = saleReturnOverview.Id,
					ReferenceType = nameof(AccountingReferenceTypes.SaleReturn),
					ReferenceNo = saleReturnOverview.TransactionNo,
					LedgerId = int.Parse(cashLedger.Value),
					Debit = null,
					Credit = saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card,
					Remarks = $"Cash Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				});

			if (saleReturnOverview.Credit > 0)
				accountingCart.Add(new()
				{
					ReferenceId = saleReturnOverview.Id,
					ReferenceType = nameof(AccountingReferenceTypes.SaleReturn),
					ReferenceNo = saleReturnOverview.TransactionNo,
					LedgerId = saleReturnOverview.PartyId.Value,
					Debit = null,
					Credit = saleReturnOverview.Credit,
					Remarks = $"Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				});
		}
		else if (saleReturn.LocationId != 1)
			if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card + saleReturnOverview.Credit > 0)
				accountingCart.Add(new()
				{
					ReferenceId = saleReturnOverview.Id,
					ReferenceType = nameof(AccountingReferenceTypes.SaleReturn),
					ReferenceNo = saleReturnOverview.TransactionNo,
					LedgerId = ledger.Id,
					Debit = null,
					Credit = saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card + saleReturnOverview.Credit,
					Remarks = $"Location Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				});

		if (saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleReturnOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.SaleReturn),
				ReferenceNo = saleReturnOverview.TransactionNo,
				LedgerId = int.Parse(saleLedger.Value),
				Debit = saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount,
				Credit = null,
				Remarks = $"Sale Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
			});

		if (saleReturnOverview.TotalExtraTaxAmount > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleReturnOverview.Id,
				ReferenceType = nameof(AccountingReferenceTypes.SaleReturn),
				ReferenceNo = saleReturnOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = saleReturnOverview.TotalExtraTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
			});

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId, sqlDataAccessTransaction);
		var accounting = new FinancialAccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = saleReturnOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = saleReturnOverview.Id,
			ReferenceNo = saleReturnOverview.TransactionNo,
			TransactionDateTime = saleReturnOverview.TransactionDateTime,
			FinancialYearId = saleReturnOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = saleReturnOverview.Remarks,
			CreatedBy = saleReturnOverview.CreatedBy,
			CreatedAt = saleReturnOverview.CreatedAt,
			CreatedFromPlatform = saleReturnOverview.CreatedFromPlatform,
			Status = true
		};

		var ledgers = FinancialAccountingData.ConvertCartToDetails(accountingCart, accounting.Id);
		accounting.Id = await FinancialAccountingData.SaveTransaction(accounting, ledgers, false, sqlDataAccessTransaction);

		saleReturn.FinancialAccountingId = accounting.Id;
		await InsertSaleReturn(saleReturn, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		SaleReturnModel saleReturn,
		bool update,
		bool recover,
		SaleReturnOverviewModel previousSaleReturn = null,
		List<SaleReturnItemOverviewModel> previousSaleReturnDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentSaleReturn = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, saleReturn.Id, sqlDataAccessTransaction);
			var currentSaleReturnDetails = await CommonData.LoadTableDataByMasterId<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, saleReturn.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousSaleReturn, currentSaleReturn);
			var detailsDiff = AuditTrailData.GetDifference(previousSaleReturnDetails, currentSaleReturnDetails, typeof(SaleReturnOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = StoreNames.SaleReturn,
			RecordNo = saleReturn.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? saleReturn.LastModifiedBy.Value : saleReturn.CreatedBy,
			CreatedFromPlatform = update ? saleReturn.LastModifiedFromPlatform : saleReturn.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
