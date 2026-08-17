using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Data.Inventory.Recipe;

public static class RecipeData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RecipeData));


	public static async Task<List<RecipeModel>> LoadAllRecipes(DateOnly date, bool deduct) =>
		await ApiClient.Get<List<RecipeModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadAllRecipes)), new { date, deduct });

	public static async Task<RecipeInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<RecipeInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });

	public static async Task DeleteTransaction(RecipeModel recipe, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), recipe, new { userId, platform });

	public static async Task<int> SaveTransaction(RecipeModel recipe, List<RecipeDetailModel> recipeDetails, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new RecipeSaveRequest(recipe, recipeDetails), new { userId, platform });
}
