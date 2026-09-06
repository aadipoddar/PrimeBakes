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
	private sealed class TransactionNoRow
	{
		public string TransactionNo { get; set; }
	}

	private sealed class CodeRow
	{
		public string Code { get; set; }
	}

	private static async Task<string> GenerateMasterCode(string tableName, string settingsKey, CodeType codeType, int numberLength, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var prefix = (await SettingsData.LoadSettingsByKey(settingsKey, sqlDataAccessTransaction)).Value;
		var codes = await CommonData.LoadTableData<CodeRow>(tableName, sqlDataAccessTransaction);

		var lastCode = codes
			.Where(c => c.Code.Length == prefix.Length + numberLength && c.Code.StartsWith(prefix))
			.Max(c => c.Code);

		var lastNumber = int.TryParse(lastCode?[prefix.Length..], out int number) ? number : 0;
		return await CheckDuplicateCode(n => $"{prefix}{n.ToString($"D{numberLength}")}", lastNumber + 1, codeType, sqlDataAccessTransaction);
	}

	private static async Task<string> GenerateTransactionNo(string tableName, string settingsKey, CodeType codeType, int financialYearId, int? locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, financialYearId, sqlDataAccessTransaction);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId ?? 1, sqlDataAccessTransaction)).Code;
		var transactionPrefix = (await SettingsData.LoadSettingsByKey(settingsKey, sqlDataAccessTransaction)).Value;
		var prefix = $"{locationPrefix}{financialYear.YearNo}{transactionPrefix}";

		var lastTransaction = locationId is null
			? await CommonData.LoadLastTableDataByFinancialYear<TransactionNoRow>(tableName, financialYearId, sqlDataAccessTransaction)
			: await CommonData.LoadLastTableDataByLocationFinancialYear<TransactionNoRow>(tableName, locationId.Value, financialYearId, sqlDataAccessTransaction);

		var lastTransactionNo = lastTransaction?.TransactionNo;
		var lastNumber = lastTransactionNo is not null
			&& lastTransactionNo.Length == prefix.Length + 6
			&& lastTransactionNo.StartsWith(prefix)
			&& int.TryParse(lastTransactionNo[prefix.Length..], out int number) ? number : 0;

		return await CheckDuplicateCode(n => $"{prefix}{n:D6}", lastNumber + 1, codeType, sqlDataAccessTransaction);
	}

	private static async Task<string> CheckDuplicateCode(Func<int, string> buildCode, int number, CodeType type, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		while (true)
		{
			var code = buildCode(number);
			var isDuplicate = false;

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

			number++;
		}
	}

	#region Accounts
	public static async Task<string> GenerateAccountingTransactionNo(FinancialAccountingModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(AccountNames.FinancialAccounting, SettingsKeys.AccountingTransactionPrefix, CodeType.Accounting, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	internal static async Task<string> GenerateLedgerCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(AccountNames.Ledger, SettingsKeys.LedgerCodePrefix, CodeType.Ledger, 5, sqlDataAccessTransaction);
	#endregion

	#region Inventory
	public static async Task<string> GeneratePurchaseTransactionNo(PurchaseModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.Purchase, SettingsKeys.PurchaseTransactionPrefix, CodeType.Purchase, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GeneratePurchaseOrderTransactionNo(PurchaseOrderModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.PurchaseOrder, SettingsKeys.PurchaseOrderTransactionPrefix, CodeType.PurchaseOrder, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GeneratePurchaseReturnTransactionNo(PurchaseReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.PurchaseReturn, SettingsKeys.PurchaseReturnTransactionPrefix, CodeType.PurchaseReturn, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.KitchenIssue, SettingsKeys.KitchenIssueTransactionPrefix, CodeType.KitchenIssue, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GenerateKitchenIssueReturnTransactionNo(KitchenIssueReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.KitchenIssueReturn, SettingsKeys.KitchenIssueReturnTransactionPrefix, CodeType.KitchenIssueReturn, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.KitchenProduction, SettingsKeys.KitchenProductionTransactionPrefix, CodeType.KitchenProduction, transaction.FinancialYearId, null, sqlDataAccessTransaction);

	public static async Task<string> GenerateKitchenProductionReturnTransactionNo(KitchenProductionReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(InventoryNames.KitchenProductionReturn, SettingsKeys.KitchenProductionReturnTransactionPrefix, CodeType.KitchenProductionReturn, transaction.FinancialYearId, null, sqlDataAccessTransaction);

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

	internal static async Task<string> GenerateRawMaterialCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(InventoryNames.RawMaterial, SettingsKeys.RawMaterialCodePrefix, CodeType.RawMaterial, 4, sqlDataAccessTransaction);
	#endregion

	#region Store
	public static async Task<string> GenerateOrderTransactionNo(OrderModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(StoreNames.Order, SettingsKeys.OrderTransactionPrefix, CodeType.Order, transaction.FinancialYearId, transaction.LocationId, sqlDataAccessTransaction);

	public static async Task<string> GenerateSaleTransactionNo(SaleModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(StoreNames.Sale, SettingsKeys.SaleTransactionPrefix, CodeType.Sale, transaction.FinancialYearId, transaction.LocationId, sqlDataAccessTransaction);

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(StoreNames.SaleReturn, SettingsKeys.SaleReturnTransactionPrefix, CodeType.SaleReturn, transaction.FinancialYearId, transaction.LocationId, sqlDataAccessTransaction);

	public static async Task<string> GenerateStockTransferTransactionNo(StockTransferModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(StoreNames.StockTransfer, SettingsKeys.StockTransferTransactionPrefix, CodeType.StockTransfer, transaction.FinancialYearId, transaction.LocationId, sqlDataAccessTransaction);

	internal static async Task<string> GenerateProductCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(StoreNames.Product, SettingsKeys.FinishedProductCodePrefix, CodeType.FinishedProduct, 4, sqlDataAccessTransaction);
	#endregion

	#region Restuarant
	public static async Task<string> GenerateBillTransactionNo(BillModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(RestaurantNames.Bill, SettingsKeys.BillTransactionPrefix, CodeType.Bill, transaction.FinancialYearId, transaction.LocationId, sqlDataAccessTransaction);
	#endregion

	#region Payroll
	internal static async Task<string> GenerateDepartmentCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(PayrollNames.Department, SettingsKeys.DepartmentCodePrefix, CodeType.Department, 4, sqlDataAccessTransaction);

	internal static async Task<string> GenerateDesignationCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(PayrollNames.Designation, SettingsKeys.DesignationCodePrefix, CodeType.Designation, 4, sqlDataAccessTransaction);

	internal static async Task<string> GenerateEmployeeCode(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateMasterCode(PayrollNames.Employee, SettingsKeys.EmployeeCodePrefix, CodeType.Employee, 4, sqlDataAccessTransaction);

	internal static async Task<string> GeneratePayrollTransactionNo(PayrollModel transaction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await GenerateTransactionNo(PayrollNames.Payroll, SettingsKeys.PayrollTransactionPrefix, CodeType.Payroll, transaction.FinancialYearId, null, sqlDataAccessTransaction);
	#endregion
}
