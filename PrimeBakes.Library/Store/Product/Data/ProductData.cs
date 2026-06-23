using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Product.Models;

namespace PrimeBakes.Library.Store.Product.Data;

public static class ProductData
{
	public static async Task<int> InsertProduct(ProductModel product, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertProduct, product, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product.");

	private static async Task SyncProductLocations(ProductModel product, List<LocationModel> locations, SqlDataAccessTransaction transaction)
	{
		var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(product.Id, null, transaction);
		productLocations = [.. productLocations.Where(pl => locations.Any(l => l.Id == pl.LocationId))];

		foreach (var location in productLocations)
			await ProductLocationData.DeleteProductLocationById(location.Id, transaction);

		foreach (var location in locations)
			await ProductLocationData.InsertProductLocation(new()
			{
				Id = 0,
				Rate = product.Rate,
				ProductId = product.Id,
				LocationId = location.Id
			}, transaction);
	}

	public static async Task DeleteTransaction(ProductModel product, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			product.Status = false;
			await InsertProduct(product, transaction);

			var productLocations = await ProductLocationData.LoadProductLocationOverviewByProductLocation(product.Id, null, transaction);
			foreach (var pl in productLocations)
				await ProductLocationData.DeleteProductLocationById(pl.Id, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.Product,
				RecordNo = product.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(ProductModel product, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			product.Status = true;
			await InsertProduct(product, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = StoreNames.Product,
				RecordNo = product.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(ProductModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.FoodType = item.FoodType?.Trim() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Product name is required. Please enter a valid name.");

		if (item.ProductCategoryId <= 0)
			throw new Exception("Category is required. Please select a category.");

		if (item.KOTCategoryId <= 0)
			throw new Exception("KOT category is required. Please select a KOT category.");

		if (item.Rate < 0)
			throw new Exception("Rate must be greater than or equal to 0.");

		if (item.TaxId <= 0)
			throw new Exception("Tax is required. Please select a tax.");

		if (!FoodTypeOptions.FoodTypes.Contains(item.FoodType))
			throw new Exception("Food type is required. Please select a valid food type.");

		var allProducts = await CommonData.LoadTableData<ProductModel>(StoreNames.Product);

		var existingByName = allProducts.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Product name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(ProductModel product, List<LocationModel> locations, int userId, string platform)
	{
		await ValidateTransaction(product);

		var isUpdate = product.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, product.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			if (!isUpdate)
				product.Code = await GenerateCodes.GenerateProductCode(transaction);

			product.Id = await InsertProduct(product, transaction);
			await SyncProductLocations(product, locations, transaction);

			var diff = AuditTrailData.GetDifference(previous, product);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.Product,
				RecordNo = product.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return product.Id;
		});
	}
}
