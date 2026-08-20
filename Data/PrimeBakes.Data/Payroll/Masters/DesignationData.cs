using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class DesignationData
{
	private static async Task<int> InsertDesignation(DesignationModel designation, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertDesignation, designation, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Designation.");

	public static async Task DeleteTransaction(DesignationModel designation, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			designation.Status = false;
			await InsertDesignation(designation, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Designation,
				RecordNo = designation.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(DesignationModel designation, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			designation.Status = true;
			await InsertDesignation(designation, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Designation,
				RecordNo = designation.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(DesignationModel item)
	{
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Designation name is required. Please enter a valid name.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateDesignationCode();

		var allDesignations = await CommonData.LoadTableData<DesignationModel>(PayrollNames.Designation);

		var existingByName = allDesignations.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Designation name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(DesignationModel designation, int userId, string platform)
	{
		await ValidateTransaction(designation);

		var isUpdate = designation.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<DesignationModel>(PayrollNames.Designation, designation.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertDesignation(designation, transaction);
			var diff = AuditTrailData.GetDifference(previous, designation);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.Designation,
				RecordNo = designation.Code,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
