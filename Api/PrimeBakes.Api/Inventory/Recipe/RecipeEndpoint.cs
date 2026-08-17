using PrimeBakes.Api.Common;
using PrimeBakes.Data.Inventory.Recipe;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Api.Inventory.Recipe;

public class RecipeEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RecipeEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(RecipeData.LoadAllRecipes),
			(DateOnly date, bool deduct) => RecipeData.LoadAllRecipes(date, deduct));

		group.MapGet(nameof(RecipeData.LoadInvoiceBundle),
			(int transactionId) => RecipeData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(RecipeData.DeleteTransaction),
			(RecipeModel recipe, int userId, string platform) => RecipeData.DeleteTransaction(recipe, userId, platform));

		group.MapPost(nameof(RecipeData.SaveTransaction),
			(RecipeSaveRequest request, int userId, string platform) => RecipeData.SaveTransaction(request.Recipe, request.Details, userId, platform));
	}
}
