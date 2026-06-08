using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class ProductLocationData
{
	public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Location.");

	public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocation(int? ProductId = null, int? LocationId = null, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoreNames.LoadProductLocationOverviewByProductLocation, new { ProductId, LocationId }, sqlDataAccessTransaction);

	public static async Task<int> DeleteProductLocationById(int id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.DeleteProductLocationById, new { Id = id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Location.");
}
