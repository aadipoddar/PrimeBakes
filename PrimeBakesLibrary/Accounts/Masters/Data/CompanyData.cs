using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail.Data;
using PrimeBakesLibrary.Operations.AuditTrail.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class CompanyData
{
	private static async Task<int> InsertCompany(CompanyModel company, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertCompany, company, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Company.");

	public static async Task DeleteTransaction(CompanyModel company, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			company.Status = false;
			await InsertCompany(company, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = AccountNames.Company,
				RecordNo = company.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(CompanyModel company, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			company.Status = true;
			await InsertCompany(company, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = AccountNames.Company,
				RecordNo = company.Name,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(CompanyModel item)
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
			throw new Exception("Company name is required. Please enter a valid company name.");

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new Exception("Company code is required. Please enter a valid company code.");

		if (item.StateUTId <= 0)
			throw new Exception("State/UT is required. Please select a valid State/UT.");

		if (!string.IsNullOrWhiteSpace(item.Phone) && !Helper.ValidatePhoneNumber(item.Phone))
			throw new Exception("Invalid phone number format. Please enter a valid phone number.");

		if (!string.IsNullOrWhiteSpace(item.Email) && !Helper.ValidateEmail(item.Email))
			throw new Exception("Invalid email format. Please enter a valid email address.");

		var allCompanies = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);

		var existingByName = allCompanies.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Company name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allCompanies.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new Exception($"Company code '{item.Code}' already exists. Please choose a different code.");
	}

	public static async Task<int> SaveTransaction(CompanyModel company, int userId, string platform)
	{
		await ValidateTransaction(company);

		var isUpdate = company.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, company.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertCompany(company, transaction);
			var diff = AuditTrailData.GetDifference(previous, company);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = AccountNames.Company,
				RecordNo = company.Name,
				RecordValue = diff,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
