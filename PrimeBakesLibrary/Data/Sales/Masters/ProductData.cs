
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Masters;

namespace PrimeBakesLibrary.Data.Sales.Masters;

public static class ProductData
{
    public static async Task<int> InsertProduct(ProductModel product, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProduct, product, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<int> InsertProductCategory(ProductCategoryModel productCategory) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductCategory, productCategory)).FirstOrDefault();

    public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<ProductLocationOverviewModel>> LoadProductRateByProduct(int ProductId, SqlDataAccessTransaction sqlDataAccessTransaction) =>
        await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoredProcedureNames.LoadProductRateByProduct, new { ProductId }, sqlDataAccessTransaction);

    public static async Task<List<ProductLocationOverviewModel>> LoadProductByLocation(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoredProcedureNames.LoadProductByLocation, new { LocationId }, sqlDataAccessTransaction);

    public static async Task<int> SaveProduct(ProductModel product)
    {
        bool isNewProduct = product.Id == 0;

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            if (isNewProduct)
                product.Code = await GenerateCodes.GenerateProductCode(sqlDataAccessTransaction);

            product.Id = await InsertProduct(product, sqlDataAccessTransaction);

            if (isNewProduct)
                await InsertNewProductLocations(product, sqlDataAccessTransaction);
            else
                await UpdateProductLocations(product, sqlDataAccessTransaction);

            sqlDataAccessTransaction.CommitTransaction();
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }

        return product.Id;
    }

    private static async Task InsertNewProductLocations(ProductModel product, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        foreach (var location in locations)
            await InsertProductLocation(new()
            {
                Id = 0,
                Rate = product.Rate,
                ProductId = product.Id,
                LocationId = location.Id,
                Status = true,
            }, sqlDataAccessTransaction);
    }

    private static async Task UpdateProductLocations(ProductModel product, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        var productLocations = await LoadProductRateByProduct(product.Id, sqlDataAccessTransaction);
        foreach (var productLocation in productLocations)
            await InsertProductLocation(new()
            {
                Id = productLocation.Id,
                Rate = product.Rate,
                ProductId = product.Id,
                LocationId = productLocation.LocationId,
                Status = true,
            }, sqlDataAccessTransaction);
    }
}