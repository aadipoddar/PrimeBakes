namespace PrimeBakes.Models.Inventory.Recipe;

public static class RecipeCartExtensions
{
	public static List<RecipeDetailModel> ConvertCartToDetails(this List<RecipeItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new RecipeDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			Status = true
		})];
}
