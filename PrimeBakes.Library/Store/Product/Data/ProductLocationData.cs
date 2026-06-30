using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Product.Models;

namespace PrimeBakes.Library.Store.Product.Data;

public static class ProductLocationData
{
	public static async Task<int> InsertProductLocation(ProductLocationModel productLocation, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProductLocation, productLocation, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Location.");

	public static async Task<int> DeleteProductLocationById(int id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.DeleteProductLocationById, new { Id = id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Location.");

	public static async Task<List<ProductLocationOverviewModel>> LoadProductLocationOverviewByProductLocationDate(int? ProductId = null, int? LocationId = null, DateOnly? Date = null, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoreNames.LoadProductLocationOverviewByProductLocationDate, new { ProductId, LocationId, Date }, sqlDataAccessTransaction);

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

	public static async Task DiscontinueTransaction(ProductLocationOverviewModel productLocation, int userId, string platform)
	{
		var existing = await LoadProductLocationOverviewByProductLocationDate(productLocation.ProductId, productLocation.LocationId);
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, productLocation.LocationId);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in existing)
				await DeleteProductLocationById(item.Id, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.ProductLocation,
				RecordNo = $"Discontinue {productLocation.Code} {location?.Name}",
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
	}

	private static async Task ValidateTransaction(ProductLocationModel item)
	{
		if (item.LocationId <= 0)
			throw new Exception("Location is required. Please select a location.");

		if (item.ProductId <= 0)
			throw new Exception("Product is required. Please select a product.");

		if (item.Rate < 0)
			throw new Exception("Rate must be greater than or equal to 0.");

		var existing = await LoadProductLocationOverviewByProductLocationDate(item.ProductId, item.LocationId);
		var duplicate = existing.FirstOrDefault(x => x.Id != item.Id && x.FromDate == item.FromDate);
		if (duplicate is not null)
			throw new Exception($"A rate for this product at this location effective {item.FromDate:dd-MMM-yyyy} already exists. Edit that entry instead.");
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
}
