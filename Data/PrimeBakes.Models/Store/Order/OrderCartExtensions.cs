namespace PrimeBakes.Models.Store.Order;

public static class OrderCartExtensions
{
	public static List<OrderDetailModel> ConvertCartToDetails(this List<OrderItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new OrderDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ItemId,
			Quantity = item.Quantity,
			Remarks = item.Remarks,
			Status = true
		})];
}
