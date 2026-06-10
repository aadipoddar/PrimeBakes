using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Store.Product.Models;

namespace PrimeBakesLibrary.Store.Product.Data;

public static class TaxData
{
	private static async Task<int> InsertTax(TaxModel tax, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertTax, tax, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Tax.");

	public static async Task DeleteTransaction(TaxModel tax, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tax.Status = false;
			await InsertTax(tax, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.Tax,
				RecordNo = tax.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(TaxModel tax, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			tax.Status = true;
			await InsertTax(tax, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = StoreNames.Tax,
				RecordNo = tax.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(TaxModel item)
	{
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Tax code is required. Please enter a valid code.");

		if (item.CGST < 0 || item.SGST < 0 || item.IGST < 0)
			throw new Exception("Tax percentages must be greater than or equal to 0.");

		var allTaxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		var existingByCode = allTaxes.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Tax code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(TaxModel tax, int userId, string platform)
	{
		await ValidateTransaction(tax);

		var isUpdate = tax.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<TaxModel>(StoreNames.Tax, tax.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertTax(tax, transaction);
			var diff = AuditTrailData.GetDifference(previous, tax);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.Tax,
				RecordNo = tax.Code,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
