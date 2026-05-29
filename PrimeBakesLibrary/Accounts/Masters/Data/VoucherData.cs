using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class VoucherData
{
	private static async Task<int> InsertVoucher(VoucherModel voucher, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertVoucher, voucher, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Voucher.");

	public static async Task DeleteTransaction(VoucherModel voucher, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			voucher.Status = false;
			await InsertVoucher(voucher, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.Voucher,
				RecordNo = voucher.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(VoucherModel voucher, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			voucher.Status = true;
			await InsertVoucher(voucher, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.Voucher,
				RecordNo = voucher.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(VoucherModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Voucher name is required. Please enter a valid voucher name.");

		var allItems = await CommonData.LoadTableData<VoucherModel>(AccountNames.Voucher);

		var existingByName = allItems.FirstOrDefault(vt => vt.Id != item.Id && vt.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Voucher name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(VoucherModel voucher, int userId, string platform)
	{
		await ValidateTransaction(voucher);

		var isUpdate = voucher.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<VoucherModel>(AccountNames.Voucher, voucher.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertVoucher(voucher, transaction);
			var diff = AuditTrailData.GetDifference(previous, voucher);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.Voucher,
				RecordNo = voucher.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
