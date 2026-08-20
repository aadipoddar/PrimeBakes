namespace PrimeBakes.Models.Payroll.Masters;

public class EmployeeSalaryComponentModel
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public int SalaryComponentId { get; set; }
	public decimal Amount { get; set; }
	public string? Formula { get; set; }
	public bool Prorate { get; set; }
	public DateOnly FromDate { get; set; }
	public string? Remarks { get; set; }
}

public class EmployeeSalaryComponentOverviewModel
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public string EmployeeCode { get; set; }
	public string EmployeeName { get; set; }
	public int LocationId { get; set; }
	public int DepartmentId { get; set; }
	public int DesignationId { get; set; }
	public int SalaryComponentId { get; set; }
	public string SalaryComponentCode { get; set; }
	public string SalaryComponentName { get; set; }
	public string SalaryComponentType { get; set; }
	public int Sequence { get; set; }
	public decimal Amount { get; set; }
	public string? Formula { get; set; }
	public string? SalaryComponentFormula { get; set; }
	public bool Prorate { get; set; }
	public DateOnly FromDate { get; set; }
	public string? Remarks { get; set; }
}
