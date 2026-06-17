namespace PrimeBakesLibrary.Common;

public static class OperationRouteNames
{
	public const string Dashboard = "/";
	public const string Login = "/login";

	public const string OutletSummaryReport = "/reports/outlet-summary";
	public const string Location = "/operations/location";
	public const string User = "/operations/user";
	public const string Settings = "/operations/settings";
	public const string LocalSettings = "/operations/local-settings";

	public const string AuditTrailReport = "/operations/audit-trail-report";
}

public static class AccountsRouteNames
{
	public const string AccountsDashboard = "/accounts";
	public const string FinancialAccounting = "/accounts/financial-accounting";

	public const string FinancialAccountingReport = "/accounts/reports/financial-accounting";
	public const string AccountingLedgerReport = "/accounts/reports/accounting-ledger";
	public const string TrialBalanceReport = "/accounts/reports/trial-balance";
	public const string ProfitAndLossReport = "/accounts/reports/profit-and-loss";
	public const string BalanceSheetReport = "/accounts/reports/balance-sheet";
	public const string BankReconciliation = "/accounts/reports/bank-reconciliation";

	public const string CompanyMaster = "/accounts/masters/company";
	public const string LedgerMaster = "/accounts/masters/ledger";
	public const string VoucherMaster = "/accounts/masters/voucher";
	public const string GroupMaster = "/accounts/masters/group";
	public const string AccountTypeMaster = "/accounts/masters/account-type";
	public const string FinancialYearMaster = "/accounts/masters/financial-year";
	public const string StateUTMaster = "/accounts/masters/state-ut";
}

public static class InventoryRouteNames
{
	public const string InventoryDashboard = "/inventory";
	public const string Purchase = "/inventory/purchase";
	public const string PurchaseReturn = "/inventory/purchase-return";
	public const string PurchaseReport = "/inventory/purchase-report";
	public const string PurchaseItemReport = "/inventory/purchase-item-report";
	public const string PurchaseReturnReport = "/inventory/purchase-return-report";
	public const string PurchaseReturnItemReport = "/inventory/purchase-return-item-report";

	public const string Kitchen = "/inventory/kitchen";
	public const string KitchenIssue = "/inventory/kitchen-issue";
	public const string KitchenProduction = "/inventory/kitchen-production";
	public const string KitchenIssueReport = "/inventory/kitchen-issue-report";
	public const string KitchenIssueItemReport = "/inventory/kitchen-item-report";
	public const string KitchenProductionReport = "/inventory/kitchen-production-report";
	public const string KitchenProductionItemReport = "/inventory/kitchen-production-item-report";

	public const string ProductStockAdjustment = "/inventory/product-stock-adjustment";
	public const string RawMaterialStockAdjustment = "/inventory/raw-material-stock-adjustment";
	public const string RawMaterialStockReport = "/inventory/raw-material-stock-report";
	public const string ProductStockReport = "/inventory/product-stock-report";
	public const string RawMaterialStockDetailReport = "/inventory/raw-material-stock-detail-report";
	public const string ProductStockDetailReport = "/inventory/product-stock-detail-report";

	public const string Recipe = "/inventory/recipe";
	public const string RecipeReport = "/inventory/recipe-report";
	public const string RecipeItemReport = "/inventory/recipe-item-report";

	public const string RawMaterial = "/inventory/raw-material";
	public const string RawMaterialCategory = "/inventory/raw-material-category";
}

public static class StoreRouteNames
{
	public const string StoreDashboard = "/store";

	public const string Order = "/store/order";
	public const string OrderMobile = "/store/order-mobile";
	public const string OrderMobileCart = "/store/order-cart-mobile";
	public const string OrderMobileConfirmation = "/store/order-confirmation-mobile";
	public const string OrderReport = "/store/order-report";
	public const string OrderItemReport = "/store/order-item-report";

	public const string Sale = "/store/sale";
	public const string SaleReturn = "/store/sale-return";
	public const string SaleMobile = "/store/sale-mobile";
	public const string SaleMobileCart = "/store/sale-cart-mobile";
	public const string SaleMobilePayment = "/store/sale-payment-mobile";
	public const string SaleMobileConfirmation = "/store/sale-confirmation-mobile";
	public const string SaleReport = "/store/sale-report";
	public const string SaleItemReport = "/store/sale-item-report";
	public const string SaleReturnReport = "/store/sale-return-report";
	public const string SaleReturnItemReport = "/store/sale-return-item-report";

	public const string StockTransfer = "/store/stock-transfer";
	public const string StockTransferReport = "/store/stock-transfer-report";
	public const string StockTransferItemReport = "/store/stock-transfer-item-report";

	public const string Product = "/store/product";
	public const string ProductCategory = "/store/product-category";
	public const string KOTCategory = "/store/kot-category";
	public const string ProductLocation = "/store/product-location";
	public const string Tax = "/store/tax";
}

public static class RestaurantRouteNames
{
	public const string RestaurantDashboard = "/restaurant";
	public const string DiningDashboard = "/restaurant/dining-dashboard";
	public const string DiningMobileDashboard = "/restaurant/dining-dashboard-mobile";

	public const string Bill = "/restaurant/bill";
	public const string BillMobile = "/restaurant/bill-mobile";
	public const string BillMobileCart = "/restaurant/bill-cart-mobile";
	public const string BillMobilePayment = "/restaurant/bill-payment-mobile";
	public const string BillMobileConfirmation = "/restaurant/bill-confirmation-mobile";
	public const string BillReport = "/restaurant/bill-report";
	public const string BillItemReport = "/restaurant/bill-item-report";

	public const string DiningArea = "/restaurant/dining-area";
	public const string DiningTable = "/restaurant/dining-table";
}