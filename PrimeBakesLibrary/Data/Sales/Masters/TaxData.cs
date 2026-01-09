using PrimeBakesLibrary.Models.Sales.Masters;

namespace PrimeBakesLibrary.Data.Sales.Masters;

public static class TaxData
{
	public static async Task<int> InsertTax(TaxModel tax) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertTax, tax)).FirstOrDefault();
}
