using PrimeBakesLibrary.Models.Restuarant.Dining;

namespace PrimeBakesLibrary.Data.Restaurant.Dining;

public static class DiningTableData
{
	public static async Task<int> InsertDiningTable(DiningTableModel diningTable) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertDiningTable, diningTable)).FirstOrDefault();
}
