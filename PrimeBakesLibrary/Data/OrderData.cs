namespace PrimeBakesLibrary.Data;

public static class OrderData
{
	public static async Task<int> OrderInsert(OrderModel orderModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderInsert", orderModel)).FirstOrDefault();

	public static async Task<int> OrderDetailInsert(OrderDetailModel orderDetailModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderDetailInsert", orderDetailModel)).FirstOrDefault();

	public static async Task OrderUpdate(int oldId, int newId) =>
			await SqlDataAccess.SaveData<dynamic>("OrderUpdate", new { OldId = oldId, NewId = newId });

	public static async Task<List<ViewOrderModel>> LoadOrdersByDate(DateTime fromDate, DateTime toDate) =>
			await SqlDataAccess.LoadData<ViewOrderModel, dynamic>("LoadOrdersByDate", new { fromDate, toDate });

	public static async Task<List<ViewOrderDetailModel>> LoadOrderDetailsByOrderId(int orderId) =>
			await SqlDataAccess.LoadData<ViewOrderDetailModel, dynamic>("LoadOrderDetailsByOrderId", new { OrderId = orderId });
}