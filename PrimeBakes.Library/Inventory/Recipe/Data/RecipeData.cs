using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Recipe.Exports;
using PrimeBakes.Library.Inventory.Recipe.Models;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Utils.Mail;

namespace PrimeBakes.Library.Inventory.Recipe.Data;

public static class RecipeData
{
	private static async Task<int> InsertRecipe(RecipeModel recipe, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertRecipe, recipe, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Recipe.");

	private static async Task<int> InsertRecipeDetail(RecipeDetailModel recipeDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertRecipeDetail, recipeDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Recipe Detail.");

	public static async Task<RecipeModel> LoadRecipeByProduct(int ProductId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<RecipeModel, dynamic>(InventoryNames.LoadRecipeByProduct, new { ProductId }, sqlDataAccessTransaction)).FirstOrDefault();

	public static List<RecipeDetailModel> ConvertCartToDetails(List<RecipeItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new RecipeDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			Status = true
		})];

	#region Delete
	public static async Task DeleteTransaction(RecipeModel recipe, int userId, string platform, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(recipe, userId, platform, transaction));
			await RecipeNotify.Notify(recipe.Id, NotifyType.Deleted);
			return;
		}

		recipe.Status = false;
		await InsertRecipe(recipe, sqlDataAccessTransaction);

		var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, recipe.ProductId, sqlDataAccessTransaction);
		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.Recipe,
			RecordNo = product.Name,
			CreatedBy = userId,
			CreatedFromPlatform = platform
		}, sqlDataAccessTransaction);
	}
	#endregion

	#region Save
	private static void ValidateTransaction(RecipeModel recipe, List<RecipeDetailModel> recipeDetails)
	{
		if (recipe.ProductId <= 0)
			throw new InvalidOperationException("Please select a product for the recipe.");

		if (recipe.Quantity <= 0)
			throw new InvalidOperationException("The recipe quantity must be greater than zero.");

		if (recipeDetails is null || recipeDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one raw material to the recipe.");

		if (recipeDetails.Any(item => item.Quantity <= 0))
			throw new InvalidOperationException("Raw material quantity must be greater than zero.");
	}

	public static async Task<int> SaveTransaction(
		RecipeModel recipe,
		List<RecipeDetailModel> recipeDetails,
		int userId,
		string platform,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = recipe.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			recipe.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(recipe, recipeDetails, userId, platform, transaction));
			await RecipeNotify.Notify(recipe.Id, update ? NotifyType.Updated : NotifyType.Created);
			return recipe.Id;
		}

		ValidateTransaction(recipe, recipeDetails);

		var previousRecipe = update ? await CommonData.LoadTableDataById<RecipeOverviewModel>(InventoryNames.RecipeOverview, recipe.Id, sqlDataAccessTransaction) : null;
		var previousRecipeDetails = update ? await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(InventoryNames.RecipeDetail, recipe.Id, sqlDataAccessTransaction) : null;

		recipe.Id = await InsertRecipe(recipe, sqlDataAccessTransaction);
		await SaveTransactionDetail(recipe, recipeDetails, update, sqlDataAccessTransaction);
		await SaveAuditTrail(recipe, update, userId, platform, previousRecipe, previousRecipeDetails, sqlDataAccessTransaction);

		return recipe.Id;
	}

	private static async Task SaveTransactionDetail(RecipeModel recipe, List<RecipeDetailModel> recipeDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingRecipeDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(InventoryNames.RecipeDetail, recipe.Id, sqlDataAccessTransaction);
			foreach (var item in existingRecipeDetails)
			{
				item.Status = false;
				await InsertRecipeDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in recipeDetails)
		{
			item.MasterId = recipe.Id;
			await InsertRecipeDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAuditTrail(
		RecipeModel recipe,
		bool update,
		int userId,
		string platform,
		RecipeOverviewModel previousRecipe = null,
		List<RecipeDetailModel> previousRecipeDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;
		var currentRecipe = await CommonData.LoadTableDataById<RecipeOverviewModel>(InventoryNames.RecipeOverview, recipe.Id, sqlDataAccessTransaction);

		if (update)
		{
			var currentRecipeDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(InventoryNames.RecipeDetail, recipe.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousRecipe, currentRecipe);
			var detailsDiff = AuditTrailData.GetDifference(previousRecipeDetails, currentRecipeDetails, typeof(RecipeOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.Recipe,
			RecordNo = currentRecipe?.ProductName ?? recipe.ProductId.ToString(),
			RecordValue = difference,
			CreatedBy = userId,
			CreatedFromPlatform = platform
		}, sqlDataAccessTransaction);
	}
	#endregion
}
