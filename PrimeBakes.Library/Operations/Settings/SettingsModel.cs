namespace PrimeBakes.Library.Operations.Settings;

public class SettingsModel
{
	public string Key { get; set; }
	public string Value { get; set; }
	public string Description { get; set; }
}

public static class SettingsKeys
{
	public static string PrimaryCompanyLinkingId => "PrimaryCompanyLinkingId";

	// Code Prefixes
	public static string RawMaterialCodePrefix => "RawMaterialCodePrefix";
	public static string FinishedProductCodePrefix => "FinishedProductCodePrefix";
	public static string LedgerCodePrefix => "LedgerCodePrefix";

	// Transaction Prefixes
	public static string AccountingTransactionPrefix => "AccountingTransactionPrefix";

	public static string PurchaseTransactionPrefix => "PurchaseTransactionPrefix";
	public static string PurchaseReturnTransactionPrefix => "PurchaseReturnTransactionPrefix";
	public static string KitchenIssueTransactionPrefix => "KitchenIssueTransactionPrefix";
	public static string KitchenProductionTransactionPrefix => "KitchenProductionTransactionPrefix";
	public static string RawMaterialStockAdjustmentTransactionPrefix => "RawMaterialStockAdjustmentTransactionPrefix";
	public static string ProductStockAdjustmentTransactionPrefix => "ProductStockAdjustmentTransactionPrefix";

	public static string SaleTransactionPrefix => "SaleTransactionPrefix";
	public static string SaleReturnTransactionPrefix => "SaleReturnTransactionPrefix";
	public static string StockTransferTransactionPrefix => "StockTransferTransactionPrefix";
	public static string OrderTransactionPrefix => "OrderTransactionPrefix";

	public static string BillTransactionPrefix => "BillTransactionPrefix";

	// Vouchers
	public static string PurchaseVoucherId => "PurchaseVoucherId";
	public static string PurchaseReturnVoucherId => "PurchaseReturnVoucherId";
	public static string SaleVoucherId => "SaleVoucherId";
	public static string SaleReturnVoucherId => "SaleReturnVoucherId";
	public static string StockTransferVoucherId => "StockTransferVoucherId";
	public static string BillVoucherId => "BillVoucherId";
	public static string BillDayCloseVoucherId => "BillDayCloseVoucherId";
	public static string SaleDayCloseVoucherId => "SaleDayCloseVoucherId";

	public static string DefaultSelectedVoucherId => "DefaultSelectedVoucherId";

	// Ledgers
	public static string PurchaseLedgerId => "PurchaseLedgerId";
	public static string SaleLedgerId => "SaleLedgerId";
	public static string StockTransferLedgerId => "StockTransferLedgerId";
	public static string BillLedgerId => "BillLedgerId";
	public static string CashLedgerId => "CashLedgerId";
	public static string CashSalesLedgerId => "CashSalesLedgerId";
	public static string GSTLedgerId => "GSTLedgerId";

	// Bank Reconciliation
	public static string BankAccountTypeId => "BankAccountTypeId";

	// Purcahse Behaviour
	public static string UpdateItemMasterRateOnPurchase => "UpdateRawMaterialMasterRateOnPurchase";
	public static string UpdateItemMasterUOMOnPurchase => "UpdateRawMaterialMasterUOMOnPurchase";

	// Kitchen Production
	public static string KitchenProductionDiscountRate => "KitchenProductionDiscountRate";

	// Report Settings
	public static string AutoRefreshReportTimer => "AutoRefreshReportTimer";
	public static string ReportWarningDays => "ReportWarningDays";

	// Notification Settings
	public static string NotificationEmail => "NotificationEmail";
}