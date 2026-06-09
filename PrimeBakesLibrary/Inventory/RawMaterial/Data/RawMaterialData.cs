using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Operations.AuditTrail;

namespace PrimeBakesLibrary.Inventory.RawMaterial.Data;

public static class RawMaterialData
{
	public static async Task<int> InsertRawMaterial(RawMaterialModel rawMaterial, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertRawMaterial, rawMaterial, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Raw Material.");

	public static async Task DeleteTransaction(RawMaterialModel rawMaterial, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			rawMaterial.Status = false;
			await InsertRawMaterial(rawMaterial, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = InventoryNames.RawMaterial,
				RecordNo = rawMaterial.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(RawMaterialModel rawMaterial, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			rawMaterial.Status = true;
			await InsertRawMaterial(rawMaterial, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = InventoryNames.RawMaterial,
				RecordNo = rawMaterial.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(RawMaterialModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.UnitOfMeasurement = item.UnitOfMeasurement?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Raw material name is required. Please enter a valid name.");

		if (item.RawMaterialCategoryId <= 0)
			throw new Exception("Category is required. Please select a category.");

		if (item.Rate < 0)
			throw new Exception("Rate must be greater than or equal to 0.");

		if (string.IsNullOrWhiteSpace(item.UnitOfMeasurement))
			throw new Exception("Unit of measurement is required. Please enter a valid unit.");

		if (item.TaxId <= 0)
			throw new Exception("Tax is required. Please select a tax.");

		var allRawMaterials = await CommonData.LoadTableData<RawMaterialModel>(InventoryNames.RawMaterial);

		var existingByName = allRawMaterials.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Raw material name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(RawMaterialModel rawMaterial, int userId, string platform)
	{
		await ValidateTransaction(rawMaterial);

		var isUpdate = rawMaterial.Id > 0;
		if (!isUpdate)
			rawMaterial.Code = await GenerateCodes.GenerateRawMaterialCode();

		var previous = isUpdate
			? await CommonData.LoadTableDataById<RawMaterialModel>(InventoryNames.RawMaterial, rawMaterial.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertRawMaterial(rawMaterial, transaction);
			var diff = AuditTrailData.GetDifference(previous, rawMaterial);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = InventoryNames.RawMaterial,
				RecordNo = rawMaterial.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}