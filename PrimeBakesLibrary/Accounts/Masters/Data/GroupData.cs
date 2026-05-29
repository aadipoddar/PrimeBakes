using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class GroupData
{
	private static async Task<int> InsertGroup(GroupModel group, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertGroup, group, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Group.");

	public static async Task DeleteTransaction(GroupModel group, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			group.Status = false;
			await InsertGroup(group, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.Group,
				RecordNo = group.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(GroupModel group, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			group.Status = true;
			await InsertGroup(group, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.Group,
				RecordNo = group.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(GroupModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Group name is required. Please enter a valid group name.");

		if (item.NatureId <= 0)
			throw new Exception("Nature is required. Please select a nature.");

		var allGroups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);

		var existingByName = allGroups.FirstOrDefault(vt => vt.Id != item.Id && vt.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Group name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(GroupModel group, int userId, string platform)
	{
		await ValidateTransaction(group);

		var isUpdate = group.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<GroupModel>(AccountNames.Group, group.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertGroup(group, transaction);
			var diff = AuditTrailData.GetDifference(previous, group);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.Group,
				RecordNo = group.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
