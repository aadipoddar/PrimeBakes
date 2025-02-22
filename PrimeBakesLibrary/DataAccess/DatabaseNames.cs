namespace PrimeBakesLibrary.DataAccess;

public static class Table
{
	public static string ItemCategory => "ItemCategory";
	public static string UserCategory => "UserCategory";
	public static string Customer => "Customer";
	public static string Item => "Item";
	public static string Order => "Order";
	public static string OrderDetail => "OrderDetail";
	public static string User => "User";
}

public static class Views
{
	public static string OrderDetails => "View_OrderDetails";
	public static string Orders => "View_Orders";
}

public static class StoredProcedure
{
	public static string LoadActiveItemsByCategory => "Load_ActiveItems_By_Category";
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByIdActive => "Load_TableData_By_Id_Active";
	public static string LoadTableDataByCode => "Load_TableData_By_Code";
	public static string LoadTableDataByCodeActive => "Load_TableData_By_Code_Active";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";

	public static string DeleteOrderDetails => "Delete_OrderDetails";
	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";
	public static string LoadOrderDetailsByOrderId => "Load_OrderDetails_By_OrderId";
	public static string LoadOrdersByDateStatus => "Load_Orders_By_Date_Status";
	public static string UpdateOrder => "Update_Order";

	public static string InsertItemCategory => "Insert_ItemCategory";
	public static string InsertUserCategory => "Insert_UserCategory";
	public static string InsertCustomer => "Insert_Customer";
	public static string InsertItem => "Insert_Item";
	public static string InsertUser => "Insert_User";

	public static string UpdateItemCategory => "Update_ItemCategory";
	public static string UpdateUserCategory => "Update_UserCategory";
	public static string UpdateCustomer => "Update_Customer";
	public static string UpdateItem => "Update_Item";
	public static string UpdateUser => "Update_User";
}