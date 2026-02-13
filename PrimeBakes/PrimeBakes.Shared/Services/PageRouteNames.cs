namespace PrimeBakes.Shared.Services;

internal static class PageRouteNames
{
    #region Operations
    public const string Dashboard = "/";
    public const string Login = "/login";
    public const string AdminDashboard = "/admin";
    public const string ReportDashboard = "/report";

    public const string Location = "/operations/location";
    public const string User = "/operations/user";
    public const string Settings = "/operations/settings";
    #endregion

    #region Accounts
    public const string AccountsDashboard = "/accounts";
    public const string FinancialAccounting = "/accounts/financial-accounting";

    public const string FinancialAccountingReport = "/accounts/reports/financial-accounting";
    public const string AccountingLedgerReport = "/accounts/reports/accounting-ledger";
    public const string TrialBalanceReport = "/accounts/reports/trial-balance";
    public const string ProfitAndLossReport = "/accounts/reports/profit-and-loss";
    public const string BalanceSheetReport = "/accounts/reports/balance-sheet";

    public const string CompanyMaster = "/accounts/masters/company";
    public const string LedgerMaster = "/accounts/masters/ledger";
    public const string VoucherMaster = "/accounts/masters/voucher";
    public const string GroupMaster = "/accounts/masters/group";
    public const string AccountTypeMaster = "/accounts/masters/account-type";
    public const string FinancialYearMaster = "/accounts/masters/financial-year";
    public const string StateUTMaster = "/accounts/masters/state-ut";
    #endregion

    #region Inventory
    public const string InventoryDashboard = "/inventory";
    public const string Purchase = "/inventory/purchase/purchase";
    public const string PurchaseReturn = "/inventory/purchase/purchase-return";
    public const string PurchaseReport = "/inventory/purchase/reports/purchase";
    public const string PurchaseItemReport = "/inventory/purchase/reports/purchase-item";
    public const string PurchaseReturnReport = "/inventory/purchase/reports/purchase-return";
    public const string PurchaseReturnItemReport = "/inventory/purchase/reports/purchase-return-item";

    public const string Kitchen = "/inventory/kitchen/kitchen";
    public const string KitchenIssue = "/inventory/kitchen/kitchen-issue";
    public const string KitchenProduction = "/inventory/kitchen/kitchen-production";
    public const string KitchenIssueReport = "/inventory/kitchen/reports/kitchen-issue";
    public const string KitchenIssueItemReport = "/inventory/kitchen/reports/kitchen-issue-item";
    public const string KitchenProductionReport = "/inventory/kitchen/reports/kitchen-production";
    public const string KitchenProductionItemReport = "/inventory/kitchen/reports/kitchen-production-item";

    public const string ProductStockAdjustment = "/inventory/stock/product-stock-adjustment";
    public const string RawMaterialStockAdjustment = "/inventory/stock/raw-material-stock-adjustment";
    public const string RawMaterialStockReport = "/inventory/stock/reports/raw-material-stock";
    public const string ProductStockReport = "/inventory/stock/reports/product-stock";

    public const string RawMaterial = "/inventory/raw-material/raw-material";
    public const string RawMaterialCategory = "/inventory/raw-material/raw-material-category";
    public const string Recipe = "/inventory/recipe/recipe";
    #endregion

    #region Store
    public const string StoreDashboard = "/store";
    public const string Order = "/store/order/order";
    public const string OrderMobile = "/store/order/mobile/order";
    public const string OrderMobileCart = "/store/order/mobile/order-cart";
    public const string OrderMobileConfirmation = "/store/order/mobile/order-confirmation";
    public const string OrderReport = "/store/order/reports/order";
    public const string ReportOrderItem = "/store/order/reports/order-item";

    public const string Sale = "/store/sale/sale";
    public const string SaleReturn = "/store/sale/sale-return";
    public const string SaleMobile = "/store/sale/mobile/sale";
    public const string SaleMobileCart = "/store/sale/mobile/sale-cart";
    public const string SaleMobileConfirmation = "/store/sale/mobile/sale-confirmation";
    public const string SaleReport = "/store/sale/reports/sale";
    public const string SaleItemReport = "/store/sale/reports/sale-item";
    public const string SaleReturnReport = "/store/sale/reports/sale-return";
    public const string SaleReturnItemReport = "/store/sale/reports/sale-return-item";

    public const string StockTransfer = "/store/stock-transfer/stock-transfer";
    public const string StockTransferReport = "/store/stock-transfer/reports/stock-transfer";
    public const string StockTransferItemReport = "/store/stock-transfer/reports/stock-transfer-item";

    public const string Product = "/store/product/product";
    public const string ProductCategory = "/store/product/product-category";
    public const string ProductLocation = "/store/product/product-location";
    public const string Tax = "/store/product/tax";
    #endregion
}
