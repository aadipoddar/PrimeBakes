using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.AuditTrail;

namespace PrimeBakes.Library.Accounts.Masters.Data;

public static class AccountTypeData
{
	private static async Task<int> InsertAccountType(AccountTypeModel accountType, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertAccountType, accountType, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Account Type.");

	public static async Task DeleteTransaction(AccountTypeModel accountType, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			accountType.Status = false;
			await InsertAccountType(accountType, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.AccountType,
				RecordNo = accountType.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(AccountTypeModel accountType, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			accountType.Status = true;
			await InsertAccountType(accountType, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.AccountType,
				RecordNo = accountType.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(AccountTypeModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Account Type name is required. Please enter a valid account type name.");

		var allTypes = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);

		var existingByName = allTypes.FirstOrDefault(vt => vt.Id != item.Id && vt.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Account Type name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(AccountTypeModel accountType, int userId, string platform)
	{
		await ValidateTransaction(accountType);

		var isUpdate = accountType.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<AccountTypeModel>(AccountNames.AccountType, accountType.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertAccountType(accountType, transaction);
			var diff = AuditTrailData.GetDifference(previous, accountType);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.AccountType,
				RecordNo = accountType.Name,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
