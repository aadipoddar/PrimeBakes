using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class SalaryComponentData
{
	private static async Task<int> InsertSalaryComponent(SalaryComponentModel salaryComponent, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertSalaryComponent, salaryComponent, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Salary Component.");

	public static async Task DeleteTransaction(SalaryComponentModel salaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			salaryComponent.Status = false;
			await InsertSalaryComponent(salaryComponent, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = salaryComponent.Code,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});

	public static async Task RecoverTransaction(SalaryComponentModel salaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			salaryComponent.Status = true;
			await InsertSalaryComponent(salaryComponent, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = salaryComponent.Code,
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});

	private static async Task ValidateTransaction(SalaryComponentModel item)
	{
		item.Name = item.Name?.Trim().ToUpper() ?? string.Empty;
		item.Code = item.Code?.Trim().ToUpper() ?? string.Empty;
		item.ComponentType = item.ComponentType?.Trim() ?? string.Empty;
		item.Formula = string.IsNullOrWhiteSpace(item.Formula) ? null : item.Formula.Trim();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (string.IsNullOrWhiteSpace(item.Name))
			throw new InvalidOperationException("Salary component name is required. Please enter a valid name.");

		if (string.IsNullOrWhiteSpace(item.Code))
			throw new InvalidOperationException("Salary component code is required. It is what formulas refer to, such as BASIC.");

		if (!item.Code.All(c => char.IsLetterOrDigit(c) || c == '_'))
			throw new InvalidOperationException("Salary component code may only contain letters, digits and underscores.");

		if (char.IsDigit(item.Code[0]))
			throw new InvalidOperationException("Salary component code cannot start with a digit.");

		if (!Enum.TryParse<SalaryComponentTypes>(item.ComponentType, out _))
			throw new InvalidOperationException("Please select a valid component type.");

		if (item.Sequence <= 0)
			throw new InvalidOperationException("Sequence must be greater than zero. It sets the order formulas are calculated in.");

		var allComponents = await CommonData.LoadTableData<SalaryComponentModel>(PayrollNames.SalaryComponent);

		var existingByName = allComponents.FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
		if (existingByName is not null)
			throw new InvalidOperationException($"Salary component name '{item.Name}' already exists. Please choose a different name.");

		var existingByCode = allComponents.FirstOrDefault(x => x.Id != item.Id && x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
		if (existingByCode is not null)
			throw new InvalidOperationException($"Salary component code '{item.Code}' already exists. Please choose a different code.");

		if (item.Formula is null)
			return;

		if (SalaryFormulaEvaluator.FormulaReferences(item.Formula, item.Code))
			throw new InvalidOperationException($"The formula cannot refer to its own code '{item.Code}'.");

		var earlierCodes = allComponents
			.Where(x => x.Id != item.Id && x.Status && x.Sequence < item.Sequence)
			.Select(x => x.Code)
			.ToList();

		var laterCodes = allComponents
			.Where(x => x.Id != item.Id && x.Status && x.Sequence >= item.Sequence)
			.Select(x => x.Code)
			.ToList();

		var referencedLater = laterCodes.FirstOrDefault(code => SalaryFormulaEvaluator.FormulaReferences(item.Formula, code));

		if (referencedLater is not null)
			throw new InvalidOperationException(
				$"The formula refers to '{referencedLater}', which is calculated at the same time or later. Give this component a higher sequence number.");

		SalaryFormulaEvaluator.ValidateFormula(item.Formula, earlierCodes);
	}

	public static async Task<int> SaveTransaction(SalaryComponentModel salaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		await ValidateTransaction(salaryComponent);

		var isUpdate = salaryComponent.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<SalaryComponentModel>(PayrollNames.SalaryComponent, salaryComponent.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertSalaryComponent(salaryComponent, transaction);
			var diff = AuditTrailData.GetDifference(previous, salaryComponent);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.SalaryComponent,
				RecordNo = salaryComponent.Code,
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
