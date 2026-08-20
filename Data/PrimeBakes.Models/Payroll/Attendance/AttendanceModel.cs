namespace PrimeBakes.Models.Payroll.Attendance;

public class AttendanceModel
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public int AttendanceMonth { get; set; }
	public int AttendanceYear { get; set; }
	public decimal DaysInMonth { get; set; }
	public decimal PresentDays { get; set; }
	public decimal WeeklyOffDays { get; set; }
	public decimal HolidayDays { get; set; }
	public decimal PaidLeaveDays { get; set; }
	public decimal UnpaidLeaveDays { get; set; }
	public decimal PaidDays { get; set; }
	public decimal OvertimeHours { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class AttendanceOverviewModel
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public string EmployeeCode { get; set; }
	public string EmployeeName { get; set; }
	public int LocationId { get; set; }
	public int DepartmentId { get; set; }
	public int DesignationId { get; set; }
	public int AttendanceMonth { get; set; }
	public int AttendanceYear { get; set; }
	public decimal DaysInMonth { get; set; }
	public decimal PresentDays { get; set; }
	public decimal WeeklyOffDays { get; set; }
	public decimal HolidayDays { get; set; }
	public decimal PaidLeaveDays { get; set; }
	public decimal UnpaidLeaveDays { get; set; }
	public decimal PaidDays { get; set; }
	public decimal OvertimeHours { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
