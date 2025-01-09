namespace PrimeBakesLibrary.Data;

public static class OrderData
{
	public static async Task<int> InsertOrder(OrderModel orderModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("Insert_Order", orderModel)).FirstOrDefault();

	public static async Task<int> InsertOrderDetail(OrderDetailModel orderDetailModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("Insert_OrderDetail", orderDetailModel)).FirstOrDefault();

	public static async Task UpdateOrder(OrderModel orderModel) =>
			await SqlDataAccess.SaveData("Update_Order", orderModel);

	public static async Task DeleteOrderDetails(int orderId) =>
		await SqlDataAccess.SaveData("Delete_OrderDetails", new { OrderId = orderId });

	public static async Task<List<ViewOrderModel>> LoadOrdersByDateStatus(DateTime fromDate, DateTime toDate, bool status) =>
			await SqlDataAccess.LoadData<ViewOrderModel, dynamic>("Load_Orders_By_Date_Status", new { fromDate, toDate, status });
}