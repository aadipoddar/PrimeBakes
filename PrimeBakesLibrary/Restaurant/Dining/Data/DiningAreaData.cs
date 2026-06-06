using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Restaurant.Dining.Models;

namespace PrimeBakesLibrary.Restaurant.Dining.Data;

public static class DiningAreaData
{
	public static async Task<int> InsertDiningArea(DiningAreaModel diningArea) =>
		(await SqlDataAccess.LoadData<int, dynamic>(RestaurantNames.InsertDiningArea, diningArea)).FirstOrDefault();
}
