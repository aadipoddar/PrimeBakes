namespace PrimeBakes.Models.Payroll.PayrollRun;

public class PayrollModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int EmployeeId { get; set; }
	public int PayrollMonth { get; set; }
	public int PayrollYear { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public int AttendanceId { get; set; }
	public decimal DaysInMonth { get; set; }
	public decimal PaidDays { get; set; }
	public decimal GrossEarnings { get; set; }
	public decimal TotalDeductions { get; set; }
	public decimal EmployerContribution { get; set; }
	public decimal NetPay { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? CreatedFormFactor { get; set; }
	public string? CreatedPlatform { get; set; }
	public decimal? CreatedLatitude { get; set; }
	public decimal? CreatedLongitude { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFormFactor { get; set; }
	public string? LastModifiedPlatform { get; set; }
	public decimal? LastModifiedLatitude { get; set; }
	public decimal? LastModifiedLongitude { get; set; }
}

public class PayrollOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }

	public int EmployeeId { get; set; }
	public string EmployeeCode { get; set; }
	public string EmployeeName { get; set; }
	public int LocationId { get; set; }
	public int DepartmentId { get; set; }
	public string DepartmentName { get; set; }
	public int DesignationId { get; set; }
	public string DesignationName { get; set; }

	public int PayrollMonth { get; set; }
	public int PayrollYear { get; set; }
	public DateTime TransactionDateTime { get; set; }

	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int AttendanceId { get; set; }
	public decimal DaysInMonth { get; set; }
	public decimal PaidDays { get; set; }

	public decimal GrossEarnings { get; set; }
	public decimal TotalDeductions { get; set; }
	public decimal EmployerContribution { get; set; }
	public decimal NetPay { get; set; }

	public string Remarks { get; set; }

	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? CreatedFormFactor { get; set; }
	public string? CreatedPlatform { get; set; }
	public decimal? CreatedLatitude { get; set; }
	public decimal? CreatedLongitude { get; set; }
	public int? LastModifiedBy { get; set; }
	public string LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFormFactor { get; set; }
	public string? LastModifiedPlatform { get; set; }
	public decimal? LastModifiedLatitude { get; set; }
	public decimal? LastModifiedLongitude { get; set; }
	public bool Status { get; set; }
}

public class PayrollDetailModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int SalaryComponentId { get; set; }
	public decimal Amount { get; set; }
	public string? Formula { get; set; }
	public bool Prorate { get; set; }
	public bool Status { get; set; }
}

public class PayrollItemOverviewModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }

	public int SalaryComponentId { get; set; }
	public string SalaryComponentCode { get; set; }
	public string SalaryComponentName { get; set; }
	public string SalaryComponentType { get; set; }
	public int Sequence { get; set; }
	public bool ShowOnPayslip { get; set; }

	public decimal Amount { get; set; }
	public string Formula { get; set; }
	public bool Prorate { get; set; }

	public string TransactionNo { get; set; }
	public int EmployeeId { get; set; }
	public string EmployeeCode { get; set; }
	public string EmployeeName { get; set; }
	public int LocationId { get; set; }
	public int DepartmentId { get; set; }
	public int DesignationId { get; set; }

	public int PayrollMonth { get; set; }
	public int PayrollYear { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public decimal DaysInMonth { get; set; }
	public decimal PaidDays { get; set; }
	public decimal NetPay { get; set; }

	public bool MasterStatus { get; set; }
}
