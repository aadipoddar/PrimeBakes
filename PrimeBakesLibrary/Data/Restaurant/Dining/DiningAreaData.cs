using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakesLibrary.Data.Restaurant.Dining;

public static class DiningAreaData
{
	public static async Task<int> InsertDiningArea(DiningAreaModel diningArea) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertDiningArea, diningArea)).FirstOrDefault();
}
