using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Data.Store.Product;

public static class ProductLocationData
{
    public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocation(int? ProductId = null, int? LocationId = null, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoredProcedureNames.LoadProductLocationOverviewByProductLocation, new { ProductId, LocationId }, sqlDataAccessTransaction);
}
