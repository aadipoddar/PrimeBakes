using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Inventory;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory;

public static class RecipeData
{
    private static async Task<int> InsertRecipe(RecipeModel recipeModel, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRecipe, recipeModel, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertRecipeDetail(RecipeDetailModel recipeDetailModel, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRecipeDetail, recipeDetailModel, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<RecipeModel> LoadRecipeByProduct(int ProductId) =>
        (await SqlDataAccess.LoadData<RecipeModel, dynamic>(StoredProcedureNames.LoadRecipeByProduct, new { ProductId })).FirstOrDefault();

    private static List<RecipeDetailModel> ConvertCartToDetails(List<RecipeItemCartModel> cart, int recipeId) =>
        [.. cart.Select(item => new RecipeDetailModel
        {
            Id = 0,
            MasterId = recipeId,
            RawMaterialId = item.ItemId,
            Quantity = item.Quantity,
            Status = true
        })];

    public static async Task DeleteRecipe(RecipeModel recipe)
    {
        recipe.Status = false;
        await InsertRecipe(recipe);
        await RecipeNotify.Notify(recipe.Id, NotifyType.Deleted);
    }

    public static async Task<int> SaveRecipe(RecipeModel recipe, List<RecipeItemCartModel> cart, bool showNotification = true)
    {
        if (cart is null || cart.Count == 0)
            throw new InvalidOperationException("Cannot save recipe with no raw materials.");

        if (cart.Any(c => c.Quantity <= 0))
            throw new InvalidOperationException("Cannot save recipe with zero or negative quantity.");

        bool update = recipe.Id > 0;
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            if (update)
            {
                var existingDetails = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, recipe.Id, sqlDataAccessTransaction);
                foreach (var detail in existingDetails)
                {
                    detail.Status = false;
                    await InsertRecipeDetail(detail, sqlDataAccessTransaction);
                }
            }

            recipe.Id = await InsertRecipe(recipe, sqlDataAccessTransaction);
            var recipeDetails = ConvertCartToDetails(cart, recipe.Id);

            foreach (var detail in recipeDetails)
            {
                detail.MasterId = recipe.Id;
                var id = await InsertRecipeDetail(detail, sqlDataAccessTransaction);

                if (id <= 0)
                    throw new InvalidOperationException("Failed to save recipe detail item.");
            }

            sqlDataAccessTransaction.CommitTransaction();
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }

        if (showNotification)
            await RecipeNotify.Notify(recipe.Id, update ? NotifyType.Updated : NotifyType.Created);

        return recipe.Id;
    }
}
