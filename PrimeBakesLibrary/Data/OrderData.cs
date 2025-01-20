namespace PrimeBakesLibrary.Data;

public static class OrderData
{
	public static async Task<int> InsertOrder(OrderModel orderModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedure.InsertOrder, orderModel)).FirstOrDefault();

	public static async Task<int> InsertOrderDetail(OrderDetailModel orderDetailModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedure.InsertOrderDetail, orderDetailModel)).FirstOrDefault();

	public static async Task UpdateOrder(OrderModel orderModel) =>
			await SqlDataAccess.SaveData(StoredProcedure.UpdateOrder, orderModel);

	public static async Task DeleteOrderDetails(int OrderId) =>
		await SqlDataAccess.SaveData(StoredProcedure.DeleteOrderDetails, new { OrderId });

	public static async Task<List<ViewOrderModel>> LoadOrdersByDateStatus(DateTime FromDate, DateTime ToDate, bool Status) =>
			await SqlDataAccess.LoadData<ViewOrderModel, dynamic>(StoredProcedure.LoadOrdersByDateStatus, new { FromDate, ToDate, Status });

	public static async Task<List<ViewOrderDetailModel>> LoadOrderDetailsByOrderId(int OrderId) =>
			await SqlDataAccess.LoadData<ViewOrderDetailModel, dynamic>(StoredProcedure.LoadOrderDetailsByOrderId, new { OrderId });
}
