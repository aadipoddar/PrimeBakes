using Dapper;

using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Payroll.Attendance;
using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Models.Payroll.PayrollRun;

using System.Data;

namespace PrimeBakes.Data.Payroll.PayrollRun;

public static class PayrollData
{
	private static async Task<int> InsertPayroll(PayrollModel payroll, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertPayroll, payroll, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Payroll.");

	private static async Task<int> InsertPayrollDetail(PayrollDetailModel payrollDetail, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertPayrollDetail, payrollDetail, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Payroll Detail.");

	private static async Task InsertPayrollDetailList(DataTable payrollDetails, SqlDataAccessTransaction transaction = null) =>
		await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertPayrollDetailList, new { PayrollDetails = payrollDetails.AsTableValuedParameter(PayrollNames.PayrollDetailType) }, transaction);

	public static async Task<List<PayrollOverviewModel>> LoadPayrollOverviewByEmployeeMonthYear(int? EmployeeId = null, int? PayrollMonth = null, int? PayrollYear = null, SqlDataAccessTransaction transaction = null) =>
		await SqlDataAccess.LoadData<PayrollOverviewModel, dynamic>(PayrollNames.LoadPayrollOverviewByEmployeeMonthYear, new { EmployeeId, PayrollMonth, PayrollYear }, transaction);

	public static async Task<PayslipBundle> LoadPayslipBundle(int payrollId)
	{
		var payroll = await CommonData.LoadTableDataById<PayrollOverviewModel>(PayrollNames.PayrollOverview, payrollId)
			?? throw new InvalidOperationException("Payroll not found.");

		var components = await CommonData.LoadTableDataByMasterId<PayrollItemOverviewModel>(PayrollNames.PayrollItemOverview, payroll.Id);
		if (components is null || components.Count == 0)
			throw new InvalidOperationException("No salary components found for the payroll.");

		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, payroll.EmployeeId)
			?? throw new InvalidOperationException("Employee information is missing.");

		var company = await SettingsData.LoadPrimaryCompany()
			?? throw new InvalidOperationException("Company information is missing.");

		return new(payroll, components, company, employee, await CommonData.LoadCurrentDateTime());
	}

	public static async Task<PayrollSaveRequest> CalculatePayroll(int employeeId, int payrollMonth, int payrollYear, SqlDataAccessTransaction transaction = null)
	{
		if (payrollMonth is < 1 or > 12)
			throw new InvalidOperationException("Please select a valid month.");

		if (payrollYear is < 2000 or > 2100)
			throw new InvalidOperationException("Please enter a valid year.");

		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, employeeId, transaction)
			?? throw new InvalidOperationException("The selected employee no longer exists.");

		var monthStart = new DateOnly(payrollYear, payrollMonth, 1);
		var monthEnd = monthStart.AddMonths(1).AddDays(-1);

		var attendance = (await AttendanceData.LoadAttendanceOverviewByEmployeeMonthYear(employeeId, payrollMonth, payrollYear, transaction))
			.FirstOrDefault(x => x.Status)
			?? throw new InvalidOperationException($"{employee.Code} has no attendance for {monthStart:MMMM yyyy}. Enter attendance first.");

		var salaryComponents = await EmployeeSalaryComponentData.LoadEffectiveSalaryComponents(employeeId, monthEnd, transaction);
		if (salaryComponents.Count == 0)
			throw new InvalidOperationException($"{employee.Code} has no salary components. Assign them first.");

		var employeeSalaryComponents = await EmployeeSalaryComponentData.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(employeeId, null, monthEnd, transaction);
		var inputAmounts = employeeSalaryComponents.ToDictionary(x => x.SalaryComponentId, x => x.Amount);

		var extraVariables = new Dictionary<string, decimal> { ["OTHOURS"] = attendance.OvertimeHours };

		var amounts = SalaryFormulaEvaluator.EvaluateAll(salaryComponents, inputAmounts, attendance.PaidDays, attendance.DaysInMonth, extraVariables);

		var transactionDateTime = monthEnd.ToDateTime(TimeOnly.MinValue);
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime, transaction)
			?? throw new InvalidOperationException($"There is no financial year covering {monthEnd:dd-MMM-yyyy}. Create it first.");

		var payroll = new PayrollModel
		{
			Id = 0,
			EmployeeId = employeeId,
			PayrollMonth = payrollMonth,
			PayrollYear = payrollYear,
			TransactionDateTime = transactionDateTime,
			FinancialYearId = financialYear.Id,
			AttendanceId = attendance.Id,
			DaysInMonth = attendance.DaysInMonth,
			PaidDays = attendance.PaidDays,
			GrossEarnings = Sum(salaryComponents, amounts, SalaryComponentTypes.Earning),
			TotalDeductions = Sum(salaryComponents, amounts, SalaryComponentTypes.Deduction),
			EmployerContribution = Sum(salaryComponents, amounts, SalaryComponentTypes.EmployerContribution),
			Status = true
		};

		payroll.NetPay = payroll.GrossEarnings - payroll.TotalDeductions;

		var payrollDetails = salaryComponents.Select(component => new PayrollDetailModel
		{
			Id = 0,
			MasterId = 0,
			SalaryComponentId = component.Id,
			Amount = amounts.GetValueOrDefault(component.Id),
			Formula = component.Formula,
			Prorate = component.Prorate,
			Status = true
		}).ToList();

		return new PayrollSaveRequest(payroll, payrollDetails);
	}

	private static decimal Sum(List<SalaryComponentModel> salaryComponents, Dictionary<int, decimal> amounts, SalaryComponentTypes componentType) =>
		salaryComponents
			.Where(component => component.ComponentType == componentType.ToString())
			.Sum(component => amounts.GetValueOrDefault(component.Id));

	private static async Task<EmployeeModel> ValidateTransaction(PayrollModel payroll, List<PayrollDetailModel> payrollDetails, bool update, SqlDataAccessTransaction transaction)
	{
		payroll.Remarks = string.IsNullOrWhiteSpace(payroll.Remarks) ? null : payroll.Remarks.Trim();

		if (payroll.EmployeeId <= 0)
			throw new InvalidOperationException("Employee is required. Please select an employee.");

		if (payroll.PayrollMonth is < 1 or > 12)
			throw new InvalidOperationException("Please select a valid month.");

		if (payroll.PayrollYear is < 2000 or > 2100)
			throw new InvalidOperationException("Please enter a valid year.");

		if (payroll.AttendanceId <= 0)
			throw new InvalidOperationException("Payroll must be linked to an attendance record.");

		if (payrollDetails is null || payrollDetails.Count == 0)
			throw new InvalidOperationException("Payroll must have at least one salary component.");

		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, payroll.EmployeeId, transaction)
			?? throw new InvalidOperationException("The selected employee no longer exists.");

		var monthStart = new DateOnly(payroll.PayrollYear, payroll.PayrollMonth, 1);

		await FinancialYearData.ValidateFinancialYear(payroll.TransactionDateTime, transaction);

		var existing = await LoadPayrollOverviewByEmployeeMonthYear(payroll.EmployeeId, payroll.PayrollMonth, payroll.PayrollYear, transaction);
		var duplicate = existing.FirstOrDefault(x => x.Id != payroll.Id && x.Status);
		if (duplicate is not null)
			throw new InvalidOperationException($"{employee.Code} already has payroll for {monthStart:MMMM yyyy}. Edit that entry instead.");

		if (update)
		{
			var existingPayroll = await CommonData.LoadTableDataById<PayrollModel>(PayrollNames.Payroll, payroll.Id, transaction)
				?? throw new InvalidOperationException("The payroll to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPayroll.TransactionDateTime, transaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, payroll.LastModifiedBy.Value, transaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify payroll.");

			payroll.TransactionNo = existingPayroll.TransactionNo;
			payroll.CreatedBy = existingPayroll.CreatedBy;
			payroll.CreatedAt = existingPayroll.CreatedAt;
			payroll.CreatedFromPlatform = existingPayroll.CreatedFromPlatform;
		}
		else
			payroll.TransactionNo = await GenerateCodes.GeneratePayrollTransactionNo(payroll, transaction);

		return employee;
	}

	public static async Task<int> SaveTransaction(PayrollModel payroll, List<PayrollDetailModel> payrollDetails, int userId, string platform, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var update = payroll.Id > 0;

		if (update)
		{
			payroll.LastModifiedBy = userId;
			payroll.LastModifiedAt = DateTime.Now;
			payroll.LastModifiedFromPlatform = platform;
		}
		else
		{
			payroll.CreatedBy = userId;
			payroll.CreatedAt = DateTime.Now;
			payroll.CreatedFromPlatform = platform;
		}

		if (sqlDataAccessTransaction is null)
			return await SqlDataAccessTransaction.Run(transaction => SaveTransaction(payroll, payrollDetails, userId, platform, transaction));

		var employee = await ValidateTransaction(payroll, payrollDetails, update, sqlDataAccessTransaction);

		var previous = update
			? await CommonData.LoadTableDataById<PayrollOverviewModel>(PayrollNames.PayrollOverview, payroll.Id, sqlDataAccessTransaction)
			: null;

		payroll.Id = await InsertPayroll(payroll, sqlDataAccessTransaction);

		List<PayrollDetailModel> details = [];

		if (update)
		{
			var existingDetails = await CommonData.LoadTableDataByMasterId<PayrollDetailModel>(PayrollNames.PayrollDetail, payroll.Id, sqlDataAccessTransaction);
			foreach (var existingDetail in existingDetails)
			{
				existingDetail.Status = false;
				details.Add(existingDetail);
			}
		}

		foreach (var payrollDetail in payrollDetails)
		{
			payrollDetail.Id = 0;
			payrollDetail.MasterId = payroll.Id;
			payrollDetail.Status = true;
			details.Add(payrollDetail);
		}

		await InsertPayrollDetailList(SqlDataAccess.ToDataTable(details), sqlDataAccessTransaction);

		var current = await CommonData.LoadTableDataById<PayrollOverviewModel>(PayrollNames.PayrollOverview, payroll.Id, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = PayrollNames.Payroll,
			RecordNo = payroll.TransactionNo,
			RecordValue = update ? AuditTrailData.GetDifference(previous, current) : null,
			CreatedBy = userId,
			CreatedFromPlatform = platform
		}, sqlDataAccessTransaction);

		return payroll.Id;
	}

	public static async Task<int> RunPayroll(int payrollMonth, int payrollYear, int userId, string platform)
	{
		if (payrollMonth is < 1 or > 12)
			throw new InvalidOperationException("Please select a valid month.");

		if (payrollYear is < 2000 or > 2100)
			throw new InvalidOperationException("Please enter a valid year.");

		var attendances = await AttendanceData.LoadAttendanceOverviewByEmployeeMonthYear(null, payrollMonth, payrollYear);
		attendances = [.. attendances.Where(x => x.Status).OrderBy(x => x.EmployeeCode)];

		if (attendances.Count == 0)
			throw new InvalidOperationException($"No attendance found for {new DateOnly(payrollYear, payrollMonth, 1):MMMM yyyy}. Enter attendance first.");

		var existing = await LoadPayrollOverviewByEmployeeMonthYear(null, payrollMonth, payrollYear);

		var processed = 0;
		foreach (var attendance in attendances)
		{
			var request = await CalculatePayroll(attendance.EmployeeId, payrollMonth, payrollYear);

			var existingPayroll = existing.FirstOrDefault(x => x.EmployeeId == attendance.EmployeeId && x.Status);
			if (existingPayroll is not null)
				request.Payroll.Id = existingPayroll.Id;

			await SaveTransaction(request.Payroll, request.PayrollDetails, userId, platform);
			processed++;
		}

		return processed;
	}

	public static async Task DeleteTransaction(PayrollModel payroll, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await FinancialYearData.ValidateFinancialYear(payroll.TransactionDateTime, transaction);

			payroll.Status = false;
			payroll.LastModifiedBy = userId;
			payroll.LastModifiedAt = DateTime.Now;
			payroll.LastModifiedFromPlatform = platform;
			await InsertPayroll(payroll, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Payroll,
				RecordNo = payroll.TransactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

	public static async Task RecoverTransaction(PayrollModel payroll, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await FinancialYearData.ValidateFinancialYear(payroll.TransactionDateTime, transaction);

			var existing = await LoadPayrollOverviewByEmployeeMonthYear(payroll.EmployeeId, payroll.PayrollMonth, payroll.PayrollYear, transaction);
			if (existing.Any(x => x.Id != payroll.Id && x.Status))
				throw new InvalidOperationException($"Payroll for {new DateOnly(payroll.PayrollYear, payroll.PayrollMonth, 1):MMMM yyyy} already exists for this employee.");

			payroll.Status = true;
			payroll.LastModifiedBy = userId;
			payroll.LastModifiedAt = DateTime.Now;
			payroll.LastModifiedFromPlatform = platform;
			await InsertPayroll(payroll, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Payroll,
				RecordNo = payroll.TransactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
}
