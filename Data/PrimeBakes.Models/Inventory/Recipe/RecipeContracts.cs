using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Inventory.Recipe;

public sealed record RecipeSaveRequest(
	RecipeModel Recipe,
	List<RecipeDetailModel> Details);

public sealed record RecipeInvoiceBundle(
	RecipeModel Transaction,
	List<RecipeItemOverviewModel> Details,
	ProductModel Product,
	DateTime CurrentDateTime);

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
