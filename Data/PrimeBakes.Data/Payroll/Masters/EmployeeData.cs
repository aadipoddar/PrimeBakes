using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class EmployeeData
{
	private static readonly string[] _genders = ["MALE", "FEMALE"];
	private static readonly string[] _paymentModes = ["BANK", "CASH", "CHEQUE"];

	private static async Task<int> InsertEmployee(EmployeeModel employee, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertEmployee, employee, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Employee.");

	public static async Task DeleteTransaction(EmployeeModel employee, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			employee.Status = false;
			await InsertEmployee(employee, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(EmployeeModel employee, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			employee.Status = true;
			await InsertEmployee(employee, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Code,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	private static async Task ValidateTransaction(EmployeeModel item)
	{
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Gender = string.IsNullOrWhiteSpace(item.Gender) ? null : item.Gender.Trim().ToUpper();
		item.FatherOrHusbandName = string.IsNullOrWhiteSpace(item.FatherOrHusbandName) ? null : item.FatherOrHusbandName.Trim().ToUpper();
		item.Phone = string.IsNullOrWhiteSpace(item.Phone) ? null : item.Phone.Trim();
		item.Email = string.IsNullOrWhiteSpace(item.Email) ? null : item.Email.Trim();
		item.Address = string.IsNullOrWhiteSpace(item.Address) ? null : item.Address.Trim();
		item.PAN = string.IsNullOrWhiteSpace(item.PAN) ? null : item.PAN.Trim().ToUpper();
		item.Aadhaar = string.IsNullOrWhiteSpace(item.Aadhaar) ? null : item.Aadhaar.Trim();
		item.PFNumber = string.IsNullOrWhiteSpace(item.PFNumber) ? null : item.PFNumber.Trim().ToUpper();
		item.UANNumber = string.IsNullOrWhiteSpace(item.UANNumber) ? null : item.UANNumber.Trim();
		item.ESINumber = string.IsNullOrWhiteSpace(item.ESINumber) ? null : item.ESINumber.Trim();
		item.BankName = string.IsNullOrWhiteSpace(item.BankName) ? null : item.BankName.Trim().ToUpper();
		item.BankAccountNumber = string.IsNullOrWhiteSpace(item.BankAccountNumber) ? null : item.BankAccountNumber.Trim();
		item.IFSC = string.IsNullOrWhiteSpace(item.IFSC) ? null : item.IFSC.Trim().ToUpper();
		item.PaymentMode = string.IsNullOrWhiteSpace(item.PaymentMode) ? null : item.PaymentMode.Trim().ToUpper();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new Exception("Employee name is required. Please enter a valid name.");

		if (item.LocationId <= 0)
			throw new Exception("Please select a location for the employee.");

		if (item.DepartmentId <= 0)
			throw new Exception("Please select a department for the employee.");

		if (item.DesignationId <= 0)
			throw new Exception("Please select a designation for the employee.");

		if (item.DateOfJoining == default)
			throw new Exception("Date of joining is required. Please select a valid date.");

		if (item.DateOfLeaving is not null && item.DateOfLeaving < item.DateOfJoining)
			throw new Exception("Date of leaving cannot be earlier than the date of joining.");

		if (item.Phone is not null && !item.Phone.ValidatePhoneNumber())
			throw new Exception("Phone number must be a valid 10 digit number.");

		if (item.Email is not null && !item.Email.ValidateEmail())
			throw new Exception("Please enter a valid email address.");

		if (item.PAN is not null && item.PAN.Length != 10)
			throw new Exception("PAN must be exactly 10 characters.");

		if (item.Aadhaar is not null && (item.Aadhaar.Length != 12 || !long.TryParse(item.Aadhaar, out _)))
			throw new Exception("Aadhaar must be exactly 12 digits.");

		if (item.IFSC is not null && item.IFSC.Length != 11)
			throw new Exception("IFSC must be exactly 11 characters.");

		if (item.Gender is not null && !_genders.Contains(item.Gender))
			throw new Exception("Gender must be Male or Female.");

		if (item.PaymentMode is not null && !_paymentModes.Contains(item.PaymentMode))
			throw new Exception("Payment mode must be Bank, Cash or Cheque.");

		if (item.Id == 0)
			item.Code = await GenerateCodes.GenerateEmployeeCode();

		var allEmployees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee);

		var existingByName = allEmployees.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new Exception($"Employee name '{item.Name}' already exists. Please choose a different name.");
	}

	public static async Task<int> SaveTransaction(EmployeeModel employee, int userId, string platform)
	{
		await ValidateTransaction(employee);

		var isUpdate = employee.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, employee.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertEmployee(employee, transaction);
			var diff = AuditTrailData.GetDifference(previous, employee);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.Employee,
				RecordNo = employee.Code,
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
