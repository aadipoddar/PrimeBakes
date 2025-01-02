namespace PrimeBakesLibrary.Data;

public static class OrderData
{
	public static async Task<int> OrderInsert(OrderModel orderModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderInsert", orderModel)).FirstOrDefault();

	public static async Task<int> OrderDetailInsert(OrderDetailModel orderDetailModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderDetailInsert", orderDetailModel)).FirstOrDefault();

	public static async Task OrderUpdate(OrderModel orderModel) =>
			await SqlDataAccess.SaveData("OrderUpdate", orderModel);

	public static async Task OrderDetailsDelete(int orderId) =>
		await SqlDataAccess.SaveData("OrderDetailsDelete", new { OrderId = orderId });

	public static async Task<List<ViewOrderModel>> LoadOrdersByDate(DateTime fromDate, DateTime toDate) =>
			await SqlDataAccess.LoadData<ViewOrderModel, dynamic>("LoadOrdersByDate", new { fromDate, toDate });

	public static async Task<List<ViewOrderDetailModel>> LoadViewOrderDetailsByOrderId(int orderId) =>
			await SqlDataAccess.LoadData<ViewOrderDetailModel, dynamic>("LoadViewOrderDetailsByOrderId", new { OrderId = orderId });

	public static async Task<List<PrintOrderDetailModel>> LoadPrintOrderDetailsByOrderId(int orderId) =>
			await SqlDataAccess.LoadData<PrintOrderDetailModel, dynamic>("LoadPrintOrderDetailsByOrderId", new { OrderId = orderId });
}