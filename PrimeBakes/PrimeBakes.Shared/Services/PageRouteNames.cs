namespace PrimeBakes.Shared.Services;

internal static class PageRouteNames
{
    public const string Dashboard = "/";
    public const string Login = "/login";

    public const string InventoryDashboard = "/inventory";
    public const string Purchase = "/inventory/purchase";
    public const string PurchaseReturn = "/inventory/purchase-return";
    public const string KitchenIssue = "/inventory/kitchen-issue";
    public const string KitchenProduction = "/inventory/kitchen-production";
    public const string ProductStockAdjustment = "/inventory/product-stock-adjustment";
    public const string RawMaterialStockAdjustment = "/inventory/raw-material-stock-adjustment";
    public const string Recipe = "/inventory/recipe";

    public const string SalesDashboard = "/sales";
    public const string Sale = "/sales/sale";
    public const string SaleMobile = "/sales/sale/mobile";
    public const string SaleMobileCart = "/sales/sale-cart/mobile";
    public const string SaleMobileConfirmation = "/sales/sale-confirmation/mobile";
    public const string SaleReturn = "/sales/sale-return";
    public const string StockTransfer = "/sales/stock-transfer";
    public const string Order = "/sales/order";
    public const string OrderMobile = "/sales/order/mobile";
    public const string OrderMobileCart = "/sales/order-cart/mobile";
    public const string OrderMobileConfirmation = "/sales/order-confirmation/mobile";

    public const string AccountsDashboard = "/accounts";
    public const string FinancialAccounting = "/accounts/financial-accounting";

    public const string ReportDashboard = "/report";
    public const string ReportPurchase = "/report/purchase";
    public const string ReportPurchaseReturn = "/report/purchase-return";
    public const string ReportPurchaseItem = "/report/purchase-item";
    public const string ReportPurchaseReturnItem = "/report/purchase-return-item";
    public const string ReportKitchenIssue = "/report/kitchen-issue";
    public const string ReportKitchenProduction = "/report/kitchen-production";
    public const string ReportKitchenIssueItem = "/report/kitchen-issue-item";
    public const string ReportKitchenProductionItem = "/report/kitchen-production-item";
    public const string ReportRawMaterialStock = "/report/raw-material-stock";
    public const string ReportProductStock = "/report/product-stock";

    public const string ReportSale = "/report/sale";
    public const string ReportSaleItem = "/report/sale-item";
    public const string ReportSaleReturn = "/report/sale-return";
    public const string ReportSaleReturnItem = "/report/sale-return-item";
    public const string ReportStockTransfer = "/report/stock-transfer";
    public const string ReportStockTransferItem = "/report/stock-transfer-item";
    public const string ReportOrder = "/report/order";
    public const string ReportOrderItem = "/report/order-item";

    public const string ReportFinancialAccounting = "/report/financial-accounting";
    public const string ReportAccountingLedger = "/report/accounting-ledger";
    public const string ReportTrialBalance = "/report/trial-balance";
    public const string ReportProfitAndLoss = "/report/profit-and-loss";
    public const string ReportBalanceSheet = "/report/balance-sheet";

    public const string AdminDashboard = "/admin";
    public const string AdminLocation = "/admin/location";
    public const string AdminRawMaterial = "/admin/raw-material";
    public const string AdminRawMaterialCategory = "/admin/raw-material-category";
    public const string AdminKitchen = "/admin/kitchen";
    public const string AdminProduct = "/admin/product";
    public const string AdminProductCategory = "/admin/product-category";
    public const string AdminProductLocation = "/admin/product-location";
    public const string AdminUser = "/admin/user";
    public const string AdminTax = "/admin/tax";
    public const string AdminCompany = "/admin/company";
    public const string AdminLedger = "/admin/ledger";
    public const string AdminVoucher = "/admin/voucher";
    public const string AdminGroup = "/admin/group";
    public const string AdminAccountType = "/admin/account-type";
    public const string AdminFinancialYear = "/admin/financial-year";
    public const string AdminStateUT = "/admin/state-ut";
    public const string AdminSettings = "/admin/settings";
}
