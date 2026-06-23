using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;

namespace PrimeBakes.Library.Operations.User;

public static class UserData
{
	private static async Task<int> InsertUser(UserModel user, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertUser, user, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert User.");

	public static async Task<UserModel> LoadUserByPasscode(int Passcode) =>
		(await SqlDataAccess.LoadData<UserModel, dynamic>(OperationNames.LoadUserByPasscode, new { Passcode })).FirstOrDefault();

	public static async Task DeleteTransaction(UserModel user, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			user.Status = false;
			await InsertUser(user, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(UserModel user, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			user.Status = true;
			await InsertUser(user, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(UserModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("User name is required. Please enter a valid user name.");

		if (item.Passcode < 1000 || item.Passcode > 9999)
			throw new Exception("Passcode must be a 4-digit number. Please enter a valid passcode.");

		if (item.LocationId <= 0)
			throw new Exception("Location is required. Please select a valid location.");

		if (!item.Accounts && !item.Inventory && !item.Store && !item.Restaurant && !item.Reports && !item.Admin)
			throw new Exception("At least one role must be assigned. Please select at least one role.");

		var allUsers = await CommonData.LoadTableData<UserModel>(OperationNames.User);

		var existingByPasscode = allUsers.FirstOrDefault(x => x.Id != item.Id && x.Passcode == item.Passcode);
		if (existingByPasscode is not null)
			throw new Exception($"Passcode '{item.Passcode:0000}' already exists. Please choose a different passcode.");

		var existingByName = allUsers.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"User name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(UserModel user, int userId, string platform)
	{
		await ValidateTransaction(user);

		var isUpdate = user.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<UserModel>(OperationNames.User, user.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertUser(user, transaction);
			var diff = AuditTrailData.GetDifference(previous, user);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
