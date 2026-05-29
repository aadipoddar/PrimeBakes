using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class StateUTData
{
	private static async Task<int> InsertStateUT(StateUTModel state, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertStateUT, state, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert State/UT.");

	public static async Task DeleteTransaction(StateUTModel state, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			state.Status = false;
			await InsertStateUT(state, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.StateUT,
				RecordNo = state.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(StateUTModel state, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			state.Status = true;
			await InsertStateUT(state, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.StateUT,
				RecordNo = state.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(StateUTModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("State/UT name is required. Please enter a valid state/UT name.");

		var allItems = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		var existingByName = allItems.FirstOrDefault(vt => vt.Id != item.Id && vt.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"State/UT name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(StateUTModel state, int userId, string platform)
	{
		await ValidateTransaction(state);

		var isUpdate = state.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<StateUTModel>(AccountNames.StateUT, state.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertStateUT(state, transaction);
			var diff = AuditTrailData.GetDifference(previous, state);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.StateUT,
				RecordNo = state.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
