using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Data.Store.Customer;

public static class OnlinePartnerData
{
	private static async Task<int> InsertOnlinePartner(OnlinePartnerModel onlinePartner, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertOnlinePartner, onlinePartner, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Online Partner.");

	public static async Task DeleteTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			if (OfflineState.Offline)
				throw new Exception("Masters cannot be changed while offline.");

			onlinePartner.Status = false;
			await InsertOnlinePartner(onlinePartner, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = StoreNames.OnlinePartner,
				RecordNo = onlinePartner.Name,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});

	public static async Task RecoverTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			if (OfflineState.Offline)
				throw new Exception("Masters cannot be changed while offline.");

			onlinePartner.Status = true;
			await InsertOnlinePartner(onlinePartner, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = StoreNames.OnlinePartner,
				RecordNo = onlinePartner.Name,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});

	private static async Task ValidateTransaction(OnlinePartnerModel item)
	{
		if (OfflineState.Offline)
			throw new Exception("Masters cannot be changed while offline.");

		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Online Partner name is required. Please enter a valid name.");

		var allCategories = await CommonData.LoadTableData<OnlinePartnerModel>(StoreNames.OnlinePartner);

		var existingByName = allCategories.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Online Partner name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(OnlinePartnerModel onlinePartner, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		await ValidateTransaction(onlinePartner);

		var isUpdate = onlinePartner.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<OnlinePartnerModel>(StoreNames.OnlinePartner, onlinePartner.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertOnlinePartner(onlinePartner, transaction);
			var diff = AuditTrailData.GetDifference(previous, onlinePartner);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = StoreNames.OnlinePartner,
				RecordNo = onlinePartner.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
			return id;
		});
	}
}
