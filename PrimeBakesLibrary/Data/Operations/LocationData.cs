using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Store.Product;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Data.Operations;

public static class LocationData
{
    public static async Task<int> InsertLocation(LocationModel location, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertLocation, location, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<LocationModel?> LoadLocationByLedgerId(int ledgerId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true, sqlDataAccessTransaction);
        return locations.FirstOrDefault(l => l.LedgerId == ledgerId);
    }

    public static async Task<int> SaveTransaction(LocationModel location, LocationModel copyLocation)
    {
        bool isNewLocation = location.Id == 0;

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            location.Id = await InsertLocation(location, sqlDataAccessTransaction);
            await InsertProducts(location, copyLocation, isNewLocation, sqlDataAccessTransaction);

            sqlDataAccessTransaction.CommitTransaction();
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }

        return location.Id;
    }

    private static async Task InsertProducts(LocationModel location, LocationModel copyLocation, bool isNewLocation, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        try
        {
            if (copyLocation is not null && copyLocation.Id > 0)
            {
                if (copyLocation.Id == location.Id)
                    return;

                var existingProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(null, location.Id, sqlDataAccessTransaction);
                foreach (var existingProductLocation in existingProductLocations)
                    await ProductLocationData.InsertProductLocation(new()
                    {
                        Id = existingProductLocation.Id,
                        ProductId = existingProductLocation.ProductId,
                        LocationId = location.Id,
                        Rate = existingProductLocation.Rate,
                        Status = false
                    }, sqlDataAccessTransaction);

                var copyProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(null, copyLocation.Id, sqlDataAccessTransaction);
                foreach (var copyProductLocation in copyProductLocations)
                    await ProductLocationData.InsertProductLocation(new()
                    {
                        Id = 0,
                        ProductId = copyProductLocation.ProductId,
                        LocationId = location.Id,
                        Rate = copyProductLocation.Rate,
                        Status = true
                    }, sqlDataAccessTransaction);
            }

            else if (isNewLocation)
            {
                var existingProductLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(null, location.Id, sqlDataAccessTransaction);
                foreach (var existingProductLocation in existingProductLocations)
                    await ProductLocationData.InsertProductLocation(new()
                    {
                        Id = existingProductLocation.Id,
                        ProductId = existingProductLocation.ProductId,
                        LocationId = location.Id,
                        Rate = existingProductLocation.Rate,
                        Status = false
                    }, sqlDataAccessTransaction);

                var products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true, sqlDataAccessTransaction);
                foreach (var product in products)
                    await ProductLocationData.InsertProductLocation(new()
                    {
                        Id = 0,
                        ProductId = product.Id,
                        LocationId = location.Id,
                        Rate = product.Rate,
                        Status = true
                    }, sqlDataAccessTransaction);
            }
        }
        catch
        {
            throw;
        }
    }
}
