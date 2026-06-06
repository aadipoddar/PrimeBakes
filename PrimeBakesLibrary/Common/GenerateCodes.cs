using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Order.Models;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Store.StockTransfer.Models;

namespace PrimeBakesLibrary.Common;

public static class GenerateCodes
{
	private static async Task<string> CheckDuplicateCode(string code, int numberLength, CodeType type, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var isDuplicate = true;
		while (isDuplicate)
		{
			switch (type)
			{
				#region Accounts
				case CodeType.Accounting:
					var accounting = await CommonData.LoadTableDataByTransactionNo<FinancialAccountingModel>(AccountNames.FinancialAccounting, code, sqlDataAccessTransaction);
					isDuplicate = accounting is not null;
					break;
				case CodeType.Ledger:
					var ledger = await CommonData.LoadTableDataByCode<LedgerModel>(AccountNames.Ledger, code, sqlDataAccessTransaction);
					isDuplicate = ledger is not null;
					break;
				#endregion

				#region Inventory
				case CodeType.Purchase:
					var purchase = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(InventoryNames.Purchase, code, sqlDataAccessTransaction);
					isDuplicate = purchase is not null;
					break;
				case CodeType.PurchaseReturn:
					var purchaseReturn = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(InventoryNames.PurchaseReturn, code, sqlDataAccessTransaction);
					isDuplicate = purchaseReturn is not null;
					break;
				case CodeType.KitchenIssue:
					var kitchenIssue = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(InventoryNames.KitchenIssue, code, sqlDataAccessTransaction);
					isDuplicate = kitchenIssue is not null;
					break;
				case CodeType.KitchenProduction:
					var kitchenProduction = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(InventoryNames.KitchenProduction, code, sqlDataAccessTransaction);
					isDuplicate = kitchenProduction is not null;
					break;
				case CodeType.RawMaterial:
					var rawMaterial = await CommonData.LoadTableDataByCode<RawMaterialModel>(InventoryNames.RawMaterial, code, sqlDataAccessTransaction);
					isDuplicate = rawMaterial is not null;
					break;
				#endregion

				#region Store
				case CodeType.Order:
					var order = await CommonData.LoadTableDataByTransactionNo<OrderModel>(StoreNames.Order, code, sqlDataAccessTransaction);
					isDuplicate = order is not null;
					break;
				case CodeType.Sale:
					var sale = await CommonData.LoadTableDataByTransactionNo<SaleModel>(StoreNames.Sale, code, sqlDataAccessTransaction);
					isDuplicate = sale is not null;
					break;
				case CodeType.SaleReturn:
					var saleReturn = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(StoreNames.SaleReturn, code, sqlDataAccessTransaction);
					isDuplicate = saleReturn is not null;
					break;
				case CodeType.StockTransfer:
					var stockTransfer = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(StoreNames.StockTransfer, code, sqlDataAccessTransaction);
					isDuplicate = stockTransfer is not null;
					break;
				case CodeType.FinishedProduct:
					var product = await CommonData.LoadTableDataByCode<ProductModel>(StoreNames.Product, code, sqlDataAccessTransaction);
					isDuplicate = product is not null;
					break;
				#endregion

				#region Restuarant
				case CodeType.Bill:
					var bill = await CommonData.LoadTableDataByTransactionNo<BillModel>(RestaurantNames.Bill, code, sqlDataAccessTransaction);
					isDuplicate = bill is not null;
					break;
				#endregion
			}

			if (!isDuplicate)
				return code;

			var prefix = code[..(code.Length - numberLength)];
			var lastNumberPart = code[(code.Length - numberLength)..];
			if (int.TryParse(lastNumberPart, out int lastNumber))
			{
				int nextNumber = lastNumber + 1;
				code = $"{prefix}{nextNumber.ToString($"D{numberLength}")}";
			}
			else
				code = $"{prefix}{1.ToString($"D{numberLength}")}";
		}
		return code;
	}

	#region Accounts
	public static async Task<string> GenerateAccountingTransactionNo(FinancialAccountingModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.AccountingTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<FinancialAccountingModel>(AccountNames.FinancialAccounting, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Accounting, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Accounting, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var transactions = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger, sqlDataAccessTransaction);
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = transactions.OrderByDescending(l => l.Id).FirstOrDefault();
		if (lastTransaction is not null)
		{
			var lastCode = lastTransaction.Code;
			if (lastCode.StartsWith(transactionPrefix))
			{
				var lastNumberPart = lastCode[transactionPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{transactionPrefix}{nextNumber:D5}", 5, CodeType.Ledger, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{transactionPrefix}00001", 5, CodeType.Ledger, sqlDataAccessTransaction);
	}
	#endregion

	#region Inventory
	public static async Task<string> GeneratePurchaseTransactionNo(PurchaseModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<PurchaseModel>(InventoryNames.Purchase, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Purchase, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Purchase, sqlDataAccessTransaction);
	}

	public static async Task<string> GeneratePurchaseReturnTransactionNo(PurchaseReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<PurchaseReturnModel>(InventoryNames.PurchaseReturn, transaction.FinancialYearId);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.PurchaseReturn, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.PurchaseReturn, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenIssueTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<KitchenIssueModel>(InventoryNames.KitchenIssue, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.KitchenIssue, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.KitchenIssue, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<KitchenProductionModel>(InventoryNames.KitchenProduction, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.KitchenProduction, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.KitchenProduction, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateProductStockAdjustmentTransactionNo(DateTime transactionDateTime, int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId, sqlDataAccessTransaction)).Code;
		var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.ProductStockAdjustmentTransactionPrefix, sqlDataAccessTransaction)).Value;
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
	}

	public static async Task<string> GenerateRawMaterialStockAdjustmentTransactionNo(DateTime transactionDateTime, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix, sqlDataAccessTransaction)).Value;
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
	}

	public static async Task<string> GenerateRawMaterialCode()
	{
		var transactions = await CommonData.LoadTableData<RawMaterialModel>(InventoryNames.RawMaterial);
		var transactionsPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialCodePrefix)).Value;

		var lastTransaction = transactions.OrderByDescending(r => r.Id).FirstOrDefault();
		if (lastTransaction is not null)
		{
			var lastCode = lastTransaction.Code;
			if (lastCode.StartsWith(transactionsPrefix))
			{
				var lastNumberPart = lastCode[transactionsPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{transactionsPrefix}{nextNumber:D4}", 4, CodeType.RawMaterial);
				}
			}
		}

		return await CheckDuplicateCode($"{transactionsPrefix}0001", 4, CodeType.RawMaterial);
	}
	#endregion

	#region Store
	public static async Task<string> GenerateOrderTransactionNo(OrderModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.OrderTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByLocationFinancialYear<OrderModel>(StoreNames.Order, transaction.LocationId, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Order, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Order, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateSaleTransactionNo(SaleModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleModel>(StoreNames.Sale, transaction.LocationId, transaction.FinancialYearId);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Sale, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Sale, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleReturnModel>(StoreNames.SaleReturn, transaction.LocationId, transaction.FinancialYearId);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.SaleReturn, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.SaleReturn, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateStockTransferTransactionNo(StockTransferModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByLocationFinancialYear<StockTransferModel>(StoreNames.StockTransfer, transaction.LocationId, transaction.FinancialYearId);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.StockTransfer, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.StockTransfer, sqlDataAccessTransaction);
	}

	public static async Task<string> GenerateProductCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var transactions = await CommonData.LoadTableData<ProductModel>(StoreNames.Product, sqlDataAccessTransaction);
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.FinishedProductCodePrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = transactions.OrderByDescending(p => p.Id).FirstOrDefault();
		if (lastTransaction is not null)
		{
			var lastProductCode = lastTransaction.Code;
			if (lastProductCode.StartsWith(transactionPrefix))
			{
				var lastNumberPart = lastProductCode[transactionPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{transactionPrefix}{nextNumber:D4}", 4, CodeType.FinishedProduct, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{transactionPrefix}0001", 4, CodeType.FinishedProduct, sqlDataAccessTransaction);
	}
	#endregion

	#region Restuarant
	public static async Task<string> GenerateBillTransactionNo(BillModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.BillTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByLocationFinancialYear<BillModel>(RestaurantNames.Bill, transaction.LocationId, transaction.FinancialYearId);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Bill, sqlDataAccessTransaction);
				}
			}
		}
		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Bill, sqlDataAccessTransaction);
	}
	#endregion
}
