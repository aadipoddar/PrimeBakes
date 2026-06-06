using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class TaxData
{
	public static async Task<int> InsertTax(TaxModel tax) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertTax, tax)).FirstOrDefault();
}
