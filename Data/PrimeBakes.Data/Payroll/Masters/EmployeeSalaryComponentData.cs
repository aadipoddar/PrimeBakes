using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class EmployeeSalaryComponentData
{
	public static async Task<int> InsertEmployeeSalaryComponent(EmployeeSalaryComponentModel employeeSalaryComponent, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertEmployeeSalaryComponent, employeeSalaryComponent, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Employee Salary Component.");

	public static async Task<int> DeleteEmployeeSalaryComponentById(int id, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.DeleteEmployeeSalaryComponentById, new { Id = id }, transaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Employee Salary Component.");

	public static async Task<List<EmployeeSalaryComponentOverviewModel>> LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(int? EmployeeId = null, int? SalaryComponentId = null, DateOnly? Date = null, SqlDataAccessTransaction transaction = null) =>
		await SqlDataAccess.LoadData<EmployeeSalaryComponentOverviewModel, dynamic>(PayrollNames.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate, new { EmployeeId, SalaryComponentId, Date }, transaction);

	public static async Task<List<SalaryComponentModel>> LoadEffectiveSalaryComponents(int employeeId, DateOnly asOn, SqlDataAccessTransaction transaction = null)
	{
		var salaryComponents = await CommonData.LoadTableDataByStatus<SalaryComponentModel>(PayrollNames.SalaryComponent, true, transaction);
		var employeeSalaryComponents = await LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(employeeId, null, asOn, transaction);

		foreach (var salaryComponent in salaryComponents)
		{
			var employeeSalaryComponent = employeeSalaryComponents.FirstOrDefault(x => x.SalaryComponentId == salaryComponent.Id);
			if (employeeSalaryComponent is null)
				continue;

			if (!string.IsNullOrWhiteSpace(employeeSalaryComponent.Formula))
				salaryComponent.Formula = employeeSalaryComponent.Formula;

			salaryComponent.Prorate = employeeSalaryComponent.Prorate;
		}

		return [.. salaryComponents.OrderBy(x => x.Sequence)];
	}

	public static async Task DeleteTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await DeleteEmployeeSalaryComponentById(employeeSalaryComponent.Id, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.EmployeeSalaryComponent,
				RecordNo = $"{employeeSalaryComponent.EmployeeCode} {employeeSalaryComponent.SalaryComponentCode}",
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});

	public static async Task DiscontinueTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		var existing = await LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(employeeSalaryComponent.EmployeeId, employeeSalaryComponent.SalaryComponentId);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in existing)
				await DeleteEmployeeSalaryComponentById(item.Id, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.EmployeeSalaryComponent,
				RecordNo = $"Discontinue {employeeSalaryComponent.EmployeeCode} {employeeSalaryComponent.SalaryComponentCode}",
				CreatedBy = userId,
				CreatedFormFactor = formFactor,
				CreatedPlatform = platform,
				CreatedLatitude = latitude,
				CreatedLongitude = longitude
			}, transaction);
		});
	}

	private static async Task<SalaryComponentModel> ValidateTransaction(EmployeeSalaryComponentModel item)
	{
		item.Formula = string.IsNullOrWhiteSpace(item.Formula) ? null : item.Formula.Trim();
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();

		if (item.EmployeeId <= 0)
			throw new InvalidOperationException("Employee is required. Please select an employee.");

		if (item.SalaryComponentId <= 0)
			throw new InvalidOperationException("Salary component is required. Please select a component.");

		if (item.Amount < 0)
			throw new InvalidOperationException("Amount must be greater than or equal to 0.");

		var allSalaryComponents = await CommonData.LoadTableDataByStatus<SalaryComponentModel>(PayrollNames.SalaryComponent);
		var salaryComponent = allSalaryComponents.FirstOrDefault(x => x.Id == item.SalaryComponentId)
			?? throw new InvalidOperationException("The selected salary component no longer exists.");

		var existing = await LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(item.EmployeeId, item.SalaryComponentId);
		var duplicate = existing.FirstOrDefault(x => x.Id != item.Id && x.FromDate == item.FromDate);
		if (duplicate is not null)
			throw new InvalidOperationException($"This employee already has {salaryComponent.Code} effective {item.FromDate:dd-MMM-yyyy}. Edit that entry instead.");

		if (item.Formula is null)
			return salaryComponent;

		if (SalaryFormulaEvaluator.FormulaReferences(item.Formula, salaryComponent.Code))
			throw new InvalidOperationException($"The formula cannot refer to its own code '{salaryComponent.Code}'.");

		var laterCode = allSalaryComponents
			.Where(x => x.Id != salaryComponent.Id && x.Sequence >= salaryComponent.Sequence)
			.Select(x => x.Code)
			.FirstOrDefault(code => SalaryFormulaEvaluator.FormulaReferences(item.Formula, code));

		if (laterCode is not null)
			throw new InvalidOperationException(
				$"The formula refers to '{laterCode}', which is calculated at the same time or later than {salaryComponent.Code}. Please use an earlier component.");

		var earlierCodes = allSalaryComponents
			.Where(x => x.Id != salaryComponent.Id && x.Sequence < salaryComponent.Sequence)
			.Select(x => x.Code)
			.ToList();

		SalaryFormulaEvaluator.ValidateFormula(item.Formula, earlierCodes);

		return salaryComponent;
	}

	public static async Task<int> SaveTransaction(EmployeeSalaryComponentModel employeeSalaryComponent, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		var salaryComponent = await ValidateTransaction(employeeSalaryComponent);
		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, employeeSalaryComponent.EmployeeId);
		var isUpdate = employeeSalaryComponent.Id > 0;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertEmployeeSalaryComponent(employeeSalaryComponent, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.EmployeeSalaryComponent,
				RecordNo = $"{employee?.Code ?? employeeSalaryComponent.EmployeeId.ToString()} {salaryComponent.Code}",
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
