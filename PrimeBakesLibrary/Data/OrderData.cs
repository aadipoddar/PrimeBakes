namespace PrimeBakesLibrary.Data;

public static class OrderData
{
	public static async Task<int> OrderInsert(OrderModel orderModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderInsert", orderModel)).FirstOrDefault();

	public static async Task<int> OrderDetailInsert(OrderDetailModel orderDetailModel) =>
			(await SqlDataAccess.LoadData<int, dynamic>("OrderDetailInsert", orderDetailModel)).FirstOrDefault();
}