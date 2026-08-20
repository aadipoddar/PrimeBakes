namespace PrimeBakes.Models.Payroll.Masters;

public class SalaryComponentModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public string ComponentType { get; set; }
	public string? Formula { get; set; }
	public int Sequence { get; set; }
	public bool Prorate { get; set; }
	public bool Rounding { get; set; }
	public bool ShowOnPayslip { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public enum SalaryComponentTypes
{
	Earning,
	Deduction,
	EmployerContribution,
	Info
}
