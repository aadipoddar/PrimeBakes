using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Data.Store.Product;

public static class ProductData
{
    public static async Task<int> InsertProduct(ProductModel product, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProduct, product, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<int> InsertProductCategory(ProductCategoryModel productCategory) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductCategory, productCategory)).FirstOrDefault();

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
            await ProductLocationData.InsertProductLocation(new()
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
        var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(product.Id, null, sqlDataAccessTransaction);
        foreach (var productLocation in productLocations)
            await ProductLocationData.InsertProductLocation(new()
            {
                Id = productLocation.Id,
                Rate = product.Rate,
                ProductId = product.Id,
                LocationId = productLocation.LocationId,
                Status = true,
            }, sqlDataAccessTransaction);
    }
}