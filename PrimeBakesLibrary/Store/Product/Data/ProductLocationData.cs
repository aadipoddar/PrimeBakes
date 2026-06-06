using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class ProductLocationData
{
	public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocation(int? ProductId = null, int? LocationId = null, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoreNames.LoadProductLocationOverviewByProductLocation, new { ProductId, LocationId }, sqlDataAccessTransaction);

	public static async Task DeleteProductLocationById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.SaveData<dynamic>(StoreNames.DeleteProductLocationById, new { Id }, sqlDataAccessTransaction);
}
