using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Data.Store.Product;

public static class TaxData
{
    public static async Task<int> InsertTax(TaxModel tax) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertTax, tax)).FirstOrDefault();
}
