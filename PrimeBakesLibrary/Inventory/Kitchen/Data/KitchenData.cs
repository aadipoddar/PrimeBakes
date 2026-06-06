using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Kitchen.Models;

namespace PrimeBakesLibrary.Inventory.Kitchen.Data;

public static class KitchenData
{
	public static async Task<int> InsertKitchen(KitchenModel kitchen) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchen, kitchen)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen.");
}
