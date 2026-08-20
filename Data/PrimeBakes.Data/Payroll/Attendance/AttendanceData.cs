using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Payroll.PayrollRun;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Payroll.Attendance;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Attendance;

public static class AttendanceData
{
	private static async Task<int> InsertAttendance(AttendanceModel attendance, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(PayrollNames.InsertAttendance, attendance, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Attendance.");

	public static async Task<List<AttendanceOverviewModel>> LoadAttendanceOverviewByEmployeeMonthYear(int? EmployeeId = null, int? AttendanceMonth = null, int? AttendanceYear = null, SqlDataAccessTransaction transaction = null) =>
		await SqlDataAccess.LoadData<AttendanceOverviewModel, dynamic>(PayrollNames.LoadAttendanceOverviewByEmployeeMonthYear, new { EmployeeId, AttendanceMonth, AttendanceYear }, transaction);

	public static async Task DeleteTransaction(AttendanceModel attendance, int userId, string platform)
	{
		var recordNo = await GetRecordNo(attendance);
		await EnsureNotProcessed(attendance);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			attendance.Status = false;
			await InsertAttendance(attendance, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = PayrollNames.Attendance,
				RecordNo = recordNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
	}

	public static async Task RecoverTransaction(AttendanceModel attendance, int userId, string platform)
	{
		var recordNo = await GetRecordNo(attendance);
		await EnsureNotProcessed(attendance);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			attendance.Status = true;
			await InsertAttendance(attendance, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Recover.ToString(),
				TableName = PayrollNames.Attendance,
				RecordNo = recordNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
	}

	private static async Task EnsureNotProcessed(AttendanceModel attendance)
	{
		var payrolls = await PayrollData.LoadPayrollOverviewByEmployeeMonthYear(attendance.EmployeeId, attendance.AttendanceMonth, attendance.AttendanceYear);
		var processed = payrolls.FirstOrDefault(x => x.Status);
		if (processed is not null)
			throw new InvalidOperationException(
				$"Payroll for {new DateOnly(attendance.AttendanceYear, attendance.AttendanceMonth, 1):MMMM yyyy} is already processed for {processed.EmployeeCode}. Delete the payroll first.");
	}

	private static async Task<string> GetRecordNo(AttendanceModel attendance)
	{
		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, attendance.EmployeeId);
		return $"{employee?.Code ?? attendance.EmployeeId.ToString()} {attendance.AttendanceMonth:00}-{attendance.AttendanceYear}";
	}

	private static async Task<EmployeeModel> ValidateTransaction(AttendanceModel item)
	{
		item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		item.Status = true;

		if (item.EmployeeId <= 0)
			throw new InvalidOperationException("Employee is required. Please select an employee.");

		if (item.AttendanceMonth is < 1 or > 12)
			throw new InvalidOperationException("Please select a valid month.");

		if (item.AttendanceYear is < 2000 or > 2100)
			throw new InvalidOperationException("Please enter a valid year.");

		if (item.PresentDays < 0 || item.WeeklyOffDays < 0 || item.HolidayDays < 0 || item.PaidLeaveDays < 0 || item.UnpaidLeaveDays < 0)
			throw new InvalidOperationException("Days cannot be negative.");

		if (item.OvertimeHours < 0)
			throw new InvalidOperationException("Overtime hours cannot be negative.");

		item.DaysInMonth = DateTime.DaysInMonth(item.AttendanceYear, item.AttendanceMonth);

		var allocated = item.PresentDays + item.WeeklyOffDays + item.HolidayDays + item.PaidLeaveDays + item.UnpaidLeaveDays;
		if (allocated != item.DaysInMonth)
			throw new InvalidOperationException(
				$"The days entered add up to {allocated:0.##} but {new DateTime(item.AttendanceYear, item.AttendanceMonth, 1):MMMM yyyy} has {item.DaysInMonth:0.##} days. Please adjust the breakup.");

		item.PaidDays = item.PresentDays + item.WeeklyOffDays + item.HolidayDays + item.PaidLeaveDays;

		var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, item.EmployeeId)
			?? throw new InvalidOperationException("The selected employee no longer exists.");

		var monthStart = new DateOnly(item.AttendanceYear, item.AttendanceMonth, 1);
		var monthEnd = monthStart.AddMonths(1).AddDays(-1);

		if (employee.DateOfJoining > monthEnd)
			throw new InvalidOperationException($"{employee.Code} joined on {employee.DateOfJoining:dd-MMM-yyyy}, after {monthStart:MMMM yyyy}.");

		if (employee.DateOfLeaving is not null && employee.DateOfLeaving < monthStart)
			throw new InvalidOperationException($"{employee.Code} left on {employee.DateOfLeaving:dd-MMM-yyyy}, before {monthStart:MMMM yyyy}.");

		var existing = await LoadAttendanceOverviewByEmployeeMonthYear(item.EmployeeId, item.AttendanceMonth, item.AttendanceYear);
		var duplicate = existing.FirstOrDefault(x => x.Id != item.Id);
		if (duplicate is not null)
			throw new InvalidOperationException($"{employee.Code} already has attendance for {monthStart:MMMM yyyy}. Edit that entry instead.");

		await EnsureNotProcessed(item);

		return employee;
	}

	public static async Task<int> SaveTransaction(AttendanceModel attendance, int userId, string platform)
	{
		var employee = await ValidateTransaction(attendance);

		var isUpdate = attendance.Id > 0;
		var previous = isUpdate
			? await CommonData.LoadTableDataById<AttendanceModel>(PayrollNames.Attendance, attendance.Id)
			: null;

		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertAttendance(attendance, transaction);
			var diff = AuditTrailData.GetDifference(previous, attendance);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = PayrollNames.Attendance,
				RecordNo = $"{employee.Code} {attendance.AttendanceMonth:00}-{attendance.AttendanceYear}",
				RecordValue = isUpdate ? diff : null,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
