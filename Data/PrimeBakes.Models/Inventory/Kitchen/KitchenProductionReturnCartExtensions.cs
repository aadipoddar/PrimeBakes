namespace PrimeBakes.Models.Inventory.Kitchen;

public static class KitchenProductionReturnCartExtensions
{
	public static List<KitchenProductionReturnDetailModel> ConvertCartToDetails(this List<KitchenProductionReturnProductCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenProductionReturnDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ProductId,
			Quantity = item.Quantity,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}
