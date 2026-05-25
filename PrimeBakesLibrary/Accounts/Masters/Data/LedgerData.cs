using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;
using PrimeBakesLibrary.Operations.Location.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class LedgerData
{
	private static async Task<int> InsertLedger(LedgerModel ledger, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertLedger, ledger, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Ledger.");

	public static async Task<LedgerModel> LoadLedgerByLocationId(int locationId, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, locationId, sqlDataAccessTransaction);
		return await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, location.LedgerId, sqlDataAccessTransaction);
	}

	public static async Task DeleteTransaction(LedgerModel ledger, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			ledger.Status = false;
			await InsertLedger(ledger, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.Ledger,
				RecordNo = ledger.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(LedgerModel ledger, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			ledger.Status = true;
			await InsertLedger(ledger, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.Ledger,
				RecordNo = ledger.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(LedgerModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.GSTNo = string.IsNullOrWhiteSpace(item.GSTNo) ? null : item.GSTNo.Trim();
		item.PANNo = string.IsNullOrWhiteSpace(item.PANNo) ? null : item.PANNo.Trim();
		item.CINNo = string.IsNullOrWhiteSpace(item.CINNo) ? null : item.CINNo.Trim();
		item.Alias = string.IsNullOrWhiteSpace(item.Alias) ? null : item.Alias.Trim();
		item.Phone = string.IsNullOrWhiteSpace(item.Phone) ? null : item.Phone.Trim();
		item.Email = string.IsNullOrWhiteSpace(item.Email) ? null : item.Email.Trim();
		item.Address = string.IsNullOrWhiteSpace(item.Address) ? null : item.Address.Trim();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Ledger name is required. Please enter a valid ledger name.");

		if (item.GroupId <= 0)
			throw new Exception("Group is required. Please select a valid group.");

		if (item.AccountTypeId <= 0)
			throw new Exception("Account Type is required. Please select a valid account type.");

		if (item.StateUTId <= 0)
			throw new Exception("State/UT is required. Please select a valid State/UT.");

		if (!string.IsNullOrWhiteSpace(item.Phone) && !Helper.ValidatePhoneNumber(item.Phone))
			throw new Exception("Invalid phone number format. Please enter a valid phone number.");

		if (!string.IsNullOrWhiteSpace(item.Email) && !Helper.ValidateEmail(item.Email))
			throw new Exception("Invalid email format. Please enter a valid email address.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateLedgerCode();

		var allLedgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);

		var existingByName = allLedgers.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Ledger name '{item.Name}' already exists. Please choose a different name.");

		if (!string.IsNullOrWhiteSpace(item.Phone))
		{
			var duplicatePhone = allLedgers.FirstOrDefault(x => x.Id != item.Id && x.Phone != null && x.Phone.Equals(item.Phone, StringComparison.OrdinalIgnoreCase));
			if (duplicatePhone is not null)
				throw new Exception($"Phone number '{item.Phone}' is already associated with another ledger. Please use a different phone number.");
		}

		if (!string.IsNullOrWhiteSpace(item.Email))
		{
			var duplicateEmail = allLedgers.FirstOrDefault(x => x.Id != item.Id && x.Email != null && x.Email.Equals(item.Email, StringComparison.OrdinalIgnoreCase));
			if (duplicateEmail is not null)
				throw new Exception($"Email '{item.Email}' is already associated with another ledger. Please use a different email address.");
		}
	}

	public static async Task<int> SaveTransaction(LedgerModel ledger, int userId, string platform)
	{
		await ValidateTransaction(ledger);

		var isUpdate = ledger.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, ledger.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertLedger(ledger, transaction);
			var diff = AuditTrailData.GetDifference(previous, ledger);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.Ledger,
				RecordNo = ledger.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
