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

	public static async Task<int> InsertKOTCategory(KOTCategoryModel kotCategory) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKOTCategory, kotCategory)).FirstOrDefault();

	public static async Task<int> SaveProduct(ProductModel product, List<LocationModel> locations)
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
				await InsertNewProductLocations(product, locations, sqlDataAccessTransaction);
			else
				await UpdateProductLocations(product, locations, sqlDataAccessTransaction);

			sqlDataAccessTransaction.CommitTransaction();
		}
		catch
		{
			sqlDataAccessTransaction.RollbackTransaction();
			throw;
		}

		return product.Id;
	}

	private static async Task InsertNewProductLocations(ProductModel product, List<LocationModel> locations, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		foreach (var location in locations)
		{
			var id = await ProductLocationData.InsertProductLocation(new()
			{
				Id = 0,
				Rate = product.Rate,
				ProductId = product.Id,
				LocationId = location.Id
			}, sqlDataAccessTransaction);

			if (id <= 0)
				throw new InvalidOperationException("Failed to insert product location.");
		}
	}

	private static async Task UpdateProductLocations(ProductModel product, List<LocationModel> locations, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(product.Id, null, sqlDataAccessTransaction);
		productLocations = [.. productLocations.Where(pl => locations.Any(l => l.Id == pl.LocationId))];

		foreach (var location in productLocations)
			await ProductLocationData.DeleteProductLocationById(location.Id, sqlDataAccessTransaction);

		foreach (var productLocation in locations)
		{
			var id = await ProductLocationData.InsertProductLocation(new()
			{
				Id = 0,
				Rate = product.Rate,
				ProductId = product.Id,
				LocationId = productLocation.Id
			}, sqlDataAccessTransaction);

			if (id <= 0)
				throw new InvalidOperationException("Failed to insert product location.");
		}
	}

	public static async Task DeleteProduct(ProductModel product)
	{
		using SqlDataAccessTransaction sqlDataAccessTransaction = new();

		try
		{
			sqlDataAccessTransaction.StartTransaction();

			product.Status = false;
			await InsertProduct(product, sqlDataAccessTransaction);

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(product.Id, null, sqlDataAccessTransaction);
			foreach (var pl in productLocations)
				await ProductLocationData.DeleteProductLocationById(pl.Id, sqlDataAccessTransaction);

			sqlDataAccessTransaction.CommitTransaction();
		}
		catch
		{
			sqlDataAccessTransaction.RollbackTransaction();
			throw;
		}
	}
}