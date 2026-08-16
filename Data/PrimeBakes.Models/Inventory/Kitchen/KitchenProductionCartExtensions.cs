namespace PrimeBakes.Models.Inventory.Kitchen;

public static class KitchenProductionCartExtensions
{
	public static List<KitchenProductionDetailModel> ConvertCartToDetails(this List<KitchenProductionProductCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenProductionDetailModel
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
