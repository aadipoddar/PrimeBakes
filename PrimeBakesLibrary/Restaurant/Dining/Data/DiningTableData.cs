using PrimeBakesLibrary.Restaurant.Dining.Models;

namespace PrimeBakesLibrary.Restaurant.Dining.Data;

public static class DiningTableData
{
	public static async Task<int> InsertDiningTable(DiningTableModel diningTable) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertDiningTable, diningTable)).FirstOrDefault();
}
