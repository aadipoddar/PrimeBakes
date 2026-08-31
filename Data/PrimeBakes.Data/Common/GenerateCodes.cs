using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Models.Payroll.PayrollRun;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Data.Common;

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
				case CodeType.PurchaseOrder:
					var purchaseOrder = await CommonData.LoadTableDataByTransactionNo<PurchaseOrderModel>(InventoryNames.PurchaseOrder, code, sqlDataAccessTransaction);
					isDuplicate = purchaseOrder is not null;
					break;
				case CodeType.PurchaseReturn:
					var purchaseReturn = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(InventoryNames.PurchaseReturn, code, sqlDataAccessTransaction);
					isDuplicate = purchaseReturn is not null;
					break;
				case CodeType.KitchenIssue:
					var kitchenIssue = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(InventoryNames.KitchenIssue, code, sqlDataAccessTransaction);
					isDuplicate = kitchenIssue is not null;
					break;
				case CodeType.KitchenIssueReturn:
					var kitchenIssueReturn = await CommonData.LoadTableDataByTransactionNo<KitchenIssueReturnModel>(InventoryNames.KitchenIssueReturn, code, sqlDataAccessTransaction);
					isDuplicate = kitchenIssueReturn is not null;
					break;
				case CodeType.KitchenProduction:
					var kitchenProduction = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(InventoryNames.KitchenProduction, code, sqlDataAccessTransaction);
					isDuplicate = kitchenProduction is not null;
					break;
				case CodeType.KitchenProductionReturn:
					var kitchenProductionReturn = await CommonData.LoadTableDataByTransactionNo<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, code, sqlDataAccessTransaction);
					isDuplicate = kitchenProductionReturn is not null;
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

				#region Payroll
				case CodeType.Department:
					var department = await CommonData.LoadTableDataByCode<DepartmentModel>(PayrollNames.Department, code, sqlDataAccessTransaction);
					isDuplicate = department is not null;
					break;
				case CodeType.Designation:
					var designation = await CommonData.LoadTableDataByCode<DesignationModel>(PayrollNames.Designation, code, sqlDataAccessTransaction);
					isDuplicate = designation is not null;
					break;
				case CodeType.Employee:
					var employee = await CommonData.LoadTableDataByCode<EmployeeModel>(PayrollNames.Employee, code, sqlDataAccessTransaction);
					isDuplicate = employee is not null;
					break;
				case CodeType.Payroll:
					var payroll = await CommonData.LoadTableDataByTransactionNo<PayrollModel>(PayrollNames.Payroll, code, sqlDataAccessTransaction);
					isDuplicate = payroll is not null;
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

	internal static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
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

	public static async Task<string> GeneratePurchaseOrderTransactionNo(PurchaseOrderModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseOrderTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<PurchaseOrderModel>(InventoryNames.PurchaseOrder, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.PurchaseOrder, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.PurchaseOrder, sqlDataAccessTransaction);
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

	public static async Task<string> GenerateKitchenIssueReturnTransactionNo(KitchenIssueReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenIssueReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<KitchenIssueReturnModel>(InventoryNames.KitchenIssueReturn, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.KitchenIssueReturn, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.KitchenIssueReturn, sqlDataAccessTransaction);
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

	public static async Task<string> GenerateKitchenProductionReturnTransactionNo(KitchenProductionReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionReturnTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.KitchenProductionReturn, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.KitchenProductionReturn, sqlDataAccessTransaction);
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

	internal static async Task<string> GenerateRawMaterialCode()
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

	internal static async Task<string> GenerateProductCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
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

	#region Payroll
	internal static async Task<string> GenerateDepartmentCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var departments = await CommonData.LoadTableData<DepartmentModel>(PayrollNames.Department, sqlDataAccessTransaction);
		var departmentsPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DepartmentCodePrefix)).Value;

		var lastDepartment = departments.OrderByDescending(x => x.Id).FirstOrDefault();
		if (lastDepartment is not null)
		{
			var lastCode = lastDepartment.Code;
			if (lastCode.StartsWith(departmentsPrefix))
			{
				var lastNumberPart = lastCode[departmentsPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{departmentsPrefix}{nextNumber:D4}", 4, CodeType.Department, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{departmentsPrefix}0001", 4, CodeType.Department, sqlDataAccessTransaction);
	}

	internal static async Task<string> GenerateDesignationCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var designations = await CommonData.LoadTableData<DesignationModel>(PayrollNames.Designation, sqlDataAccessTransaction);
		var designationsPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.DesignationCodePrefix)).Value;

		var lastDesignation = designations.OrderByDescending(x => x.Id).FirstOrDefault();
		if (lastDesignation is not null)
		{
			var lastCode = lastDesignation.Code;
			if (lastCode.StartsWith(designationsPrefix))
			{
				var lastNumberPart = lastCode[designationsPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{designationsPrefix}{nextNumber:D4}", 4, CodeType.Designation, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{designationsPrefix}0001", 4, CodeType.Designation, sqlDataAccessTransaction);
	}

	internal static async Task<string> GenerateEmployeeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var employees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee, sqlDataAccessTransaction);
		var employeesPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.EmployeeCodePrefix)).Value;

		var lastEmployee = employees.OrderByDescending(e => e.Id).FirstOrDefault();
		if (lastEmployee is not null)
		{
			var lastCode = lastEmployee.Code;
			if (lastCode.StartsWith(employeesPrefix))
			{
				var lastNumberPart = lastCode[employeesPrefix.Length..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{employeesPrefix}{nextNumber:D4}", 4, CodeType.Employee, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{employeesPrefix}0001", 4, CodeType.Employee, sqlDataAccessTransaction);
	}

	internal static async Task<string> GeneratePayrollTransactionNo(PayrollModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, transaction.FinancialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PayrollTransactionPrefix, sqlDataAccessTransaction)).Value;

		var lastTransaction = await CommonData.LoadLastTableDataByFinancialYear<PayrollModel>(PayrollNames.Payroll, transaction.FinancialYearId, sqlDataAccessTransaction);
		if (lastTransaction is not null)
		{
			var lastTransactionNo = lastTransaction.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + transactionPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}{nextNumber:D6}", 6, CodeType.Payroll, sqlDataAccessTransaction);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{transactionPrefix}000001", 6, CodeType.Payroll, sqlDataAccessTransaction);
	}
	#endregion
}
