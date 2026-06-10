using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class ProductLocationData
{
	public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Location.");

	public static async Task<int> DeleteProductLocationById(int id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.DeleteProductLocationById, new { Id = id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Location.");

	public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocation(int? ProductId = null, int? LocationId = null, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoreNames.LoadProductLocationOverviewByProductLocation, new { ProductId, LocationId }, sqlDataAccessTransaction);

	private static async Task ValidateTransaction(ProductLocationModel item)
	{
		if (item.LocationId <= 0)
			throw new Exception("Location is required. Please select a location.");

		if (item.ProductId <= 0)
			throw new Exception("Product is required. Please select a product.");

		if (item.Rate < 0)
			throw new Exception("Rate must be greater than or equal to 0.");

		var existing = await LoadProductLocationOverviewByProductLocation(item.ProductId, item.LocationId);
		var match = existing.FirstOrDefault();
		if (match is not null)
			item.Id = match.Id;
	}

	public static async Task<int> SaveTransaction(ProductLocationModel productLocation, int userId, string platform)
	{
		await ValidateTransaction(productLocation);

		var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, productLocation.ProductId);
		var isUpdate = productLocation.Id > 0;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertProductLocation(productLocation, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.ProductLocation,
				RecordNo = product?.Code ?? productLocation.ProductId.ToString(),
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}

	public static async Task DeleteTransaction(ProductLocationOverviewModel productLocation, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await DeleteProductLocationById(productLocation.Id, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.ProductLocation,
				RecordNo = productLocation.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
}
