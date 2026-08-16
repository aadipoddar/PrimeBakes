namespace PrimeBakes.Models.Inventory.Kitchen;

public static class KitchenIssueReturnCartExtensions
{
	public static List<KitchenIssueReturnDetailModel> ConvertCartToDetails(this List<KitchenIssueReturnItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenIssueReturnDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}
