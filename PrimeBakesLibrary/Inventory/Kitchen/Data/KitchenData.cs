using PrimeBakesLibrary.Inventory.Kitchen.Models;

namespace PrimeBakesLibrary.Inventory.Kitchen.Data;

public static class KitchenData
{
    public static async Task<int> InsertKitchen(KitchenModel kitchen) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchen, kitchen)).FirstOrDefault();
}
