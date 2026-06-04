using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Inventory;

internal static class RecipeNotify
{
    internal static async Task Notify(int recipeId, NotifyType type)
    {
        await RecipeNotification(recipeId, type);

        if (type != NotifyType.Created)
            await RecipeMail(recipeId, type);
    }

    private static async Task RecipeNotification(int recipeId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var recipe = await CommonData.LoadTableDataById<RecipeModel>(TableNames.Recipe, recipeId);
        var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, recipe.ProductId);
        var recipeDetails = await LoadRecipeDetails(recipeId);

        var notificationData = new NotificationUtil.TransactionNotificationData
        {
            TransactionType = "Recipe",
            TransactionNo = null,
            Action = type,
            LocationName = product.Name,
            Details = new Dictionary<string, string>
            {
                ["🍴 Product"] = product.Name,
                ["📦 Raw Materials"] = recipeDetails.Count.ToString(),
                ["🔢 Total Quantity"] = recipeDetails.Sum(d => d.Quantity).FormatSmartDecimal()
            },
            Remarks = null
        };

        await NotificationUtil.SendTransactionNotification(users, notificationData);
    }

    private static async Task RecipeMail(int recipeId, NotifyType type)
    {
        var recipe = await CommonData.LoadTableDataById<RecipeModel>(TableNames.Recipe, recipeId);
        var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, recipe.ProductId);
        var recipeDetails = await LoadRecipeDetails(recipeId);

        var emailData = new MailingUtil.TransactionEmailData
        {
            TransactionType = "Recipe",
            TransactionNo = null,
            Action = type,
            LocationName = product.Name,
            Details = new Dictionary<string, string>
            {
                ["Product"] = product.Name,
                ["Product Code"] = product.Code,
                ["Total Raw Materials"] = recipeDetails.Count.ToString(),
                ["Total Quantity Required"] = recipeDetails.Sum(d => d.Quantity).FormatSmartDecimal(),
                ["Raw Materials"] = string.Join(", ", recipeDetails.Select(d => d.ItemName))
            },
            Remarks = null
        };

        await MailingUtil.SendTransactionEmail(emailData);
    }

    private static async Task<List<RecipeItemCartModel>> LoadRecipeDetails(int recipeId)
    {
        var details = await CommonData.LoadTableDataByMasterId<RecipeDetailModel>(TableNames.RecipeDetail, recipeId);
        var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        return [.. details.Select(detail => new RecipeItemCartModel
        {
            ItemId = detail.RawMaterialId,
            ItemName= rawMaterials.FirstOrDefault(rm => rm.Id == detail.RawMaterialId)?.Name ?? "Unknown",
            Quantity = detail.Quantity
        })];
    }
}
