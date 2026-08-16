using PrimeBakes.Library.Inventory.Recipe.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Recipe;

namespace PrimeBakes.Api.Inventory.Recipe.Data;

public class RecipeDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(RecipeDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(RecipeData.LoadAllRecipes),
			(DateOnly date, bool deduct) => RecipeData.LoadAllRecipes(date, deduct));

		group.MapPost(nameof(RecipeData.DeleteTransaction),
			(RecipeModel recipe, int userId, string platform) => RecipeData.DeleteTransaction(recipe, userId, platform));

		group.MapPost(nameof(RecipeData.SaveTransaction),
			(RecipeSaveRequest request, int userId, string platform) => RecipeData.SaveTransaction(request.Recipe, request.Details, userId, platform));
	}
}
