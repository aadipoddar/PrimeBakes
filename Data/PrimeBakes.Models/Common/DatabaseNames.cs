namespace PrimeBakes.Models.Common;

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
	public static string LoadDatabaseLoad => "Load_DatabaseLoad";
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
	public static string DeleteAuditTrailByDate => "Delete_AuditTrail_By_Date";
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

public static class AnalysisNames
{
	#region Dashboard
	public static string LoadDashboardMonthlyTrend => "Load_Dashboard_MonthlyTrend";
	public static string LoadDashboardTopProducts => "Load_Dashboard_TopProducts";
	public static string LoadDashboardTopRawMaterials => "Load_Dashboard_TopRawMaterials";
	#endregion
}

public static class AccountNames
{
	#region Financial Accounting
	public static string FinancialAccounting => "FinancialAccounting";
	public static string FinancialAccountingLedger => "FinancialAccountingLedger";
	public static string FinancialAccountingLedgerType => "FinancialAccountingLedgerType";

	public static string InsertFinancialAccounting => "Insert_FinancialAccounting";
	public static string InsertFinancialAccountingLedger => "Insert_FinancialAccountingLedger";
	public static string InsertFinancialAccountingLedgerList => "Insert_FinancialAccountingLedger_List";

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
	public static string PurchaseDetailType => "PurchaseDetailType";
	public static string PurchaseReturn => "PurchaseReturn";
	public static string PurchaseReturnDetail => "PurchaseReturnDetail";
	public static string PurchaseReturnDetailType => "PurchaseReturnDetailType";
	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";
	public static string InsertPurchaseDetailList => "Insert_PurchaseDetail_List";
	public static string InsertPurchaseReturn => "Insert_PurchaseReturn";
	public static string InsertPurchaseReturnDetail => "Insert_PurchaseReturnDetail";
	public static string InsertPurchaseReturnDetailList => "Insert_PurchaseReturnDetail_List";

	public static string PurchaseOverview => "Purchase_Overview";
	public static string PurchaseReturnOverview => "PurchaseReturn_Overview";
	public static string PurchaseItemOverview => "Purchase_Item_Overview";
	public static string PurchaseReturnItemOverview => "PurchaseReturn_Item_Overview";
	#endregion

	#region Purchase Order
	public static string PurchaseOrder => "PurchaseOrder";
	public static string PurchaseOrderDetail => "PurchaseOrderDetail";
	public static string PurchaseOrderDetailType => "PurchaseOrderDetailType";
	public static string InsertPurchaseOrder => "Insert_PurchaseOrder";
	public static string InsertPurchaseOrderDetail => "Insert_PurchaseOrderDetail";
	public static string InsertPurchaseOrderDetailList => "Insert_PurchaseOrderDetail_List";

	public static string PurchaseOrderOverview => "PurchaseOrder_Overview";
	public static string PurchaseOrderItemOverview => "PurchaseOrder_Item_Overview";

	public static string LoadPurchaseOrderByPartyPending => "Load_PurchaseOrder_By_Party_Pending";
	#endregion

	#region Kitchen
	public static string Kitchen => "Kitchen";
	public static string KitchenIssue => "KitchenIssue";
	public static string KitchenIssueDetail => "KitchenIssueDetail";
	public static string KitchenIssueDetailType => "KitchenIssueDetailType";
	public static string KitchenIssueReturn => "KitchenIssueReturn";
	public static string KitchenIssueReturnDetail => "KitchenIssueReturnDetail";
	public static string KitchenIssueReturnDetailType => "KitchenIssueReturnDetailType";
	public static string KitchenProduction => "KitchenProduction";
	public static string KitchenProductionDetail => "KitchenProductionDetail";
	public static string KitchenProductionDetailType => "KitchenProductionDetailType";
	public static string KitchenProductionReturn => "KitchenProductionReturn";
	public static string KitchenProductionReturnDetail => "KitchenProductionReturnDetail";
	public static string KitchenProductionReturnDetailType => "KitchenProductionReturnDetailType";

	public static string InsertKitchen => "Insert_Kitchen";
	public static string InsertKitchenIssue => "Insert_KitchenIssue";
	public static string InsertKitchenIssueDetail => "Insert_KitchenIssueDetail";
	public static string InsertKitchenIssueDetailList => "Insert_KitchenIssueDetail_List";
	public static string InsertKitchenIssueReturn => "Insert_KitchenIssueReturn";
	public static string InsertKitchenIssueReturnDetail => "Insert_KitchenIssueReturnDetail";
	public static string InsertKitchenIssueReturnDetailList => "Insert_KitchenIssueReturnDetail_List";
	public static string InsertKitchenProduction => "Insert_KitchenProduction";
	public static string InsertKitchenProductionDetail => "Insert_KitchenProductionDetail";
	public static string InsertKitchenProductionDetailList => "Insert_KitchenProductionDetail_List";
	public static string InsertKitchenProductionReturn => "Insert_KitchenProductionReturn";
	public static string InsertKitchenProductionReturnDetail => "Insert_KitchenProductionReturnDetail";
	public static string InsertKitchenProductionReturnDetailList => "Insert_KitchenProductionReturnDetail_List";

	public static string KitchenIssueOverview => "KitchenIssue_Overview";
	public static string KitchenIssueReturnOverview => "KitchenIssueReturn_Overview";
	public static string KitchenProductionOverview => "KitchenProduction_Overview";
	public static string KitchenProductionReturnOverview => "KitchenProductionReturn_Overview";
	public static string KitchenIssueItemOverview => "KitchenIssue_Item_Overview";
	public static string KitchenIssueReturnItemOverview => "KitchenIssueReturn_Item_Overview";
	public static string KitchenProductionItemOverview => "KitchenProduction_Item_Overview";
	public static string KitchenProductionReturnItemOverview => "KitchenProductionReturn_Item_Overview";
	#endregion

	#region Stock
	public static string ProductStock => "ProductStock";
	public static string ProductStockType => "ProductStockType";
	public static string RawMaterialStock => "RawMaterialStock";
	public static string RawMaterialStockType => "RawMaterialStockType";

	public static string InsertProductStock => "Insert_ProductStock";
	public static string InsertProductStockList => "Insert_ProductStock_List";
	public static string InsertRawMaterialStock => "Insert_RawMaterialStock";
	public static string InsertRawMaterialStockList => "Insert_RawMaterialStock_List";

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
	public static string OrderDetailType => "OrderDetailType";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";
	public static string InsertOrderDetailList => "Insert_OrderDetail_List";

	public static string OrderOverview => "Order_Overview";
	public static string OrderItemOverview => "Order_Item_Overview";

	public static string LoadOrderByLocationPending => "Load_Order_By_Location_Pending";
	#endregion

	#region Sale
	public static string Sale => "Sale";
	public static string SaleDetail => "SaleDetail";
	public static string SaleDetailType => "SaleDetailType";
	public static string SaleReturn => "SaleReturn";
	public static string SaleReturnDetail => "SaleReturnDetail";
	public static string SaleReturnDetailType => "SaleReturnDetailType";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";
	public static string InsertSaleDetailList => "Insert_SaleDetail_List";
	public static string InsertSaleReturn => "Insert_SaleReturn";
	public static string InsertSaleReturnDetail => "Insert_SaleReturnDetail";
	public static string InsertSaleReturnDetailList => "Insert_SaleReturnDetail_List";

	public static string SaleOverview => "Sale_Overview";
	public static string SaleItemOverview => "Sale_Item_Overview";
	public static string SaleReturnOverview => "SaleReturn_Overview";
	public static string SaleReturnItemOverview => "SaleReturn_Item_Overview";
	#endregion

	#region Stock Transfer
	public static string StockTransfer => "StockTransfer";
	public static string StockTransferDetail => "StockTransferDetail";
	public static string StockTransferDetailType => "StockTransferDetailType";

	public static string InsertStockTransfer => "Insert_StockTransfer";
	public static string InsertStockTransferDetail => "Insert_StockTransferDetail";
	public static string InsertStockTransferDetailList => "Insert_StockTransferDetail_List";

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
	public static string BillDetailType => "BillDetailType";

	public static string InsertBill => "Insert_Bill";
	public static string InsertBillDetail => "Insert_BillDetail";
	public static string InsertBillDetailList => "Insert_BillDetail_List";

	public static string BillOverview => "Bill_Overview";
	public static string BillItemOverview => "Bill_Item_Overview";

	public static string LoadRunningBillByLocationId => "Load_RunningBill_By_LocationId";

	public static string DeleteBillDetailById => "Delete_BillDetail_By_Id";
	#endregion
}

public static class PayrollNames
{
	#region Masters
	public static string Department => "Department";
	public static string Designation => "Designation";
	public static string Employee => "Employee";
	public static string SalaryComponent => "SalaryComponent";
	public static string EmployeeSalaryComponent => "EmployeeSalaryComponent";
	public static string EmployeeSalaryComponentOverview => "EmployeeSalaryComponent_Overview";
	public static string InsertDepartment => "Insert_Department";
	public static string InsertDesignation => "Insert_Designation";
	public static string InsertEmployee => "Insert_Employee";
	public static string InsertSalaryComponent => "Insert_SalaryComponent";
	public static string InsertEmployeeSalaryComponent => "Insert_EmployeeSalaryComponent";
	public static string DeleteEmployeeSalaryComponentById => "Delete_EmployeeSalaryComponent_By_Id";
	public static string LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate => "Load_EmployeeSalaryComponent_Overview_By_Employee_SalaryComponent_Date";
	#endregion

	#region Attendance
	public static string Attendance => "Attendance";
	public static string AttendanceOverview => "Attendance_Overview";
	public static string InsertAttendance => "Insert_Attendance";
	public static string LoadAttendanceOverviewByEmployeeMonthYear => "Load_Attendance_Overview_By_Employee_Month_Year";
	#endregion

	#region Payroll
	public static string Payroll => "Payroll";
	public static string PayrollDetail => "PayrollDetail";
	public static string PayrollDetailType => "PayrollDetailType";
	public static string PayrollOverview => "Payroll_Overview";
	public static string PayrollItemOverview => "Payroll_Item_Overview";
	public static string InsertPayroll => "Insert_Payroll";
	public static string InsertPayrollDetail => "Insert_PayrollDetail";
	public static string InsertPayrollDetailList => "Insert_PayrollDetail_List";
	public static string LoadPayrollOverviewByEmployeeMonthYear => "Load_Payroll_Overview_By_Employee_Month_Year";
	#endregion
}
