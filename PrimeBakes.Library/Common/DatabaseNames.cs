namespace PrimeBakes.Library.Common;

public static class CommonNames
{
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";
	public static string LoadTableDataByMasterId => "Load_TableData_By_MasterId";
	public static string LoadTableDataByFinancialAccountingId => "Load_TableData_By_FinancialAccountingId";
	public static string LoadTableDataByCode => "Load_TableData_By_Code";
	public static string LoadTableDataByTransactionNo => "Load_TableData_By_TransactionNo";
	public static string LoadTableDataByDate => "Load_TableData_By_Date";
	public static string LoadLastTableData => "Load_LastTableData";
	public static string LoadLastTableDataByFinancialYear => "Load_LastTableData_By_FinancialYear";
	public static string LoadLastTableDataByCompanyFinancialYear => "Load_LastTableData_By_Company_FinancialYear";
	public static string LoadLastTableDataByLocationFinancialYear => "Load_LastTableData_By_Location_FinancialYear";
	public static string LoadCurrentDateTime => "Load_CurrentDateTime";
}

public static class OperationNames
{
	#region Settings
	public static string Settings => "Settings";
	public static string UpdateSettings => "Update_Settings";
	public static string LoadSettingsByKey => "Load_Settings_By_Key";
	public static string ResetSettings => "Reset_Settings";
	public const string LocalSettings = "/operations/local-settings";
	#endregion

	#region Audit Trail
	public static string AuditTrail => "AuditTrail";
	public static string InsertAuditTrail => "Insert_AuditTrail";
	public static string LoadLastAuditTrailByTableRecord => "Load_Last_AuditTrail_By_Table_Record";
	#endregion

	#region User
	public static string User => "User";
	public static string InsertUser => "Insert_User";
	public static string LoadUserByPasscode => "Load_User_By_Passcode";
	#endregion

	#region Location
	public static string Location => "Location";
	public static string InsertLocation => "Insert_Location";
	#endregion

	#region Auth
	public const string Dashboard = "/";
	public const string Login = "/login";
	public const string OperationsDashboard = "/operations";
	public const string ReportDashboard = "/report";

	public const string OutletSummaryReport = "/reports/outlet-summary";
	#endregion
}

public static class AccountNames
{
	#region Financial Accounting
	public static string FinancialAccounting => "FinancialAccounting";
	public static string FinancialAccountingLedger => "FinancialAccountingLedger";

	public static string InsertFinancialAccounting => "Insert_FinancialAccounting";
	public static string InsertFinancialAccountingLedger => "Insert_FinancialAccountingLedger";

	public static string LoadFinancialAccountingByVoucherReference => "Load_FinancialAccounting_By_Voucher_Reference";
	public static string LoadTrialBalanceByCompanyDate => "Load_TrialBalance_By_Company_Date";

	public static string FinancialAccountingOverview => "FinancialAccounting_Overview";
	public static string FinancialAccountingLedgerOverview => "FinancialAccounting_Ledger_Overview";
	#endregion

	#region Masters
	public static string Company => "Company";
	public static string Group => "Group";
	public static string AccountType => "AccountType";
	public static string StateUT => "StateUT";
	public static string Ledger => "Ledger";
	public static string Voucher => "Voucher";
	public static string FinancialYear => "FinancialYear";

	public static string InsertCompany => "Insert_Company";
	public static string InsertGroup => "Insert_Group";
	public static string InsertAccountType => "Insert_AccountType";
	public static string InsertStateUT => "Insert_StateUT";
	public static string InsertLedger => "Insert_Ledger";
	public static string InsertVoucher => "Insert_Voucher";
	public static string InsertFinancialYear => "Insert_FinancialYear";

	public static string LoadFinancialYearByDateTime => "Load_FinancialYear_By_DateTime";
	#endregion
}

public static class InventoryNames
{
	#region Purchase
	public static string Purchase => "Purchase";
	public static string PurchaseDetail => "PurchaseDetail";
	public static string PurchaseReturn => "PurchaseReturn";
	public static string PurchaseReturnDetail => "PurchaseReturnDetail";
	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";
	public static string InsertPurchaseReturn => "Insert_PurchaseReturn";
	public static string InsertPurchaseReturnDetail => "Insert_PurchaseReturnDetail";

	public static string PurchaseOverview => "Purchase_Overview";
	public static string PurchaseReturnOverview => "PurchaseReturn_Overview";
	public static string PurchaseItemOverview => "Purchase_Item_Overview";
	public static string PurchaseReturnItemOverview => "PurchaseReturn_Item_Overview";
	#endregion

	#region Kitchen
	public static string Kitchen => "Kitchen";
	public static string KitchenIssue => "KitchenIssue";
	public static string KitchenIssueDetail => "KitchenIssueDetail";
	public static string KitchenProduction => "KitchenProduction";
	public static string KitchenProductionDetail => "KitchenProductionDetail";

	public static string InsertKitchen => "Insert_Kitchen";
	public static string InsertKitchenIssue => "Insert_KitchenIssue";
	public static string InsertKitchenIssueDetail => "Insert_KitchenIssueDetail";
	public static string InsertKitchenProduction => "Insert_KitchenProduction";
	public static string InsertKitchenProductionDetail => "Insert_KitchenProductionDetail";

	public static string KitchenIssueOverview => "KitchenIssue_Overview";
	public static string KitchenProductionOverview => "KitchenProduction_Overview";
	public static string KitchenIssueItemOverview => "KitchenIssue_Item_Overview";
	public static string KitchenProductionItemOverview => "KitchenProduction_Item_Overview";
	#endregion

	#region Stock
	public static string ProductStock => "ProductStock";
	public static string RawMaterialStock => "RawMaterialStock";

	public static string InsertProductStock => "Insert_ProductStock";
	public static string InsertRawMaterialStock => "Insert_RawMaterialStock";

	public static string RawMaterialStockDetails => "RawMaterialStockDetails";
	public static string ProductStockDetails => "ProductStockDetails";

	public static string LoadRawMaterialOpeningStockByDate => "Load_RawMaterial_OpeningStock_By_Date";
	public static string LoadProductOpeningStockByDateLocationId => "Load_Product_OpeningStock_By_Date_LocationId";

	public static string DeleteProductStockById => "Delete_ProductStock_By_Id";
	public static string DeleteProductStockByTransactionNo => "Delete_ProductStock_By_TransactionNo";
	public static string DeleteRawMaterialStockById => "Delete_RawMaterialStock_By_Id";
	public static string DeleteRawMaterialStockByTransactionNo => "Delete_RawMaterialStock_By_TransactionNo";
	#endregion

	#region Recipe
	public static string Recipe => "Recipe";
	public static string RecipeDetail => "RecipeDetail";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

	public static string RecipeOverview => "Recipe_Overview";
	public static string RecipeItemOverview => "Recipe_Item_Overview";
	#endregion

	#region Raw Material
	public static string RawMaterialCategory => "RawMaterialCategory";
	public static string RawMaterial => "RawMaterial";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string LoadRawMaterialByPartyPurchaseDateTime => "Load_RawMaterial_By_Party_PurchaseDateTime";
	#endregion
}

public static class StoreNames
{
	#region Order
	public static string Order => "Order";
	public static string OrderDetail => "OrderDetail";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";

	public static string OrderOverview => "Order_Overview";
	public static string OrderItemOverview => "Order_Item_Overview";

	public static string LoadOrderByLocationPending => "Load_Order_By_Location_Pending";
	#endregion

	#region Sale
	public static string Sale => "Sale";
	public static string SaleDetail => "SaleDetail";
	public static string SaleReturn => "SaleReturn";
	public static string SaleReturnDetail => "SaleReturnDetail";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";
	public static string InsertSaleReturn => "Insert_SaleReturn";
	public static string InsertSaleReturnDetail => "Insert_SaleReturnDetail";

	public static string SaleOverview => "Sale_Overview";
	public static string SaleItemOverview => "Sale_Item_Overview";
	public static string SaleReturnOverview => "SaleReturn_Overview";
	public static string SaleReturnItemOverview => "SaleReturn_Item_Overview";
	#endregion

	#region Stock Transfer
	public static string StockTransfer => "StockTransfer";
	public static string StockTransferDetail => "StockTransferDetail";

	public static string InsertStockTransfer => "Insert_StockTransfer";
	public static string InsertStockTransferDetail => "Insert_StockTransferDetail";

	public static string StockTransferOverview => "StockTransfer_Overview";
	public static string StockTransferItemOverview => "StockTransfer_Item_Overview";
	#endregion

	#region Product
	public static string ProductCategory => "ProductCategory";
	public static string KOTCategory => "KOTCategory";
	public static string Product => "Product";
	public static string ProductLocation => "ProductLocation";
	public static string Tax => "Tax";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertKOTCategory => "Insert_KOTCategory";
	public static string InsertProduct => "Insert_Product";
	public static string InsertProductLocation => "Insert_ProductLocation";
	public static string InsertTax => "Insert_Tax";

	public static string ProductLocationOverview => "ProductLocation_Overview";

	public static string DeleteProductLocationById => "Delete_ProductLocation_By_Id";

	public static string LoadProductLocationOverviewByProductLocationDate => "Load_ProductLocation_Overview_By_Product_Location_Date";
	#endregion

	#region Customer
	public static string Customer => "Customer";
	public static string InsertCustomer => "Insert_Customer";
	public static string LoadCustomerByNumber => "Load_Customer_By_Number";
	#endregion
}

public static class RestaurantNames
{
	#region Dining
	public static string DiningArea => "DiningArea";
	public static string DiningTable => "DiningTable";

	public static string InsertDiningArea => "Insert_DiningArea";
	public static string InsertDiningTable => "Insert_DiningTable";
	#endregion

	#region Bill
	public static string Bill => "Bill";
	public static string BillDetail => "BillDetail";

	public static string InsertBill => "Insert_Bill";
	public static string InsertBillDetail => "Insert_BillDetail";

	public static string BillOverview => "Bill_Overview";
	public static string BillItemOverview => "Bill_Item_Overview";

	public static string LoadRunningBillByLocationId => "Load_RunningBill_By_LocationId";

	public static string DeleteBillDetailById => "Delete_BillDetail_By_Id";
	#endregion
}