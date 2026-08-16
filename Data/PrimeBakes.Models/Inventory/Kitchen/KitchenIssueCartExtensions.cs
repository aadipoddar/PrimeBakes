namespace PrimeBakes.Models.Inventory.Kitchen;

public static class KitchenIssueCartExtensions
{
	public static List<KitchenIssueDetailModel> ConvertCartToDetails(this List<KitchenIssueItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenIssueDetailModel
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
