namespace PrimeBakes.Models.Payroll.Masters;

public class EmployeeModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public int LocationId { get; set; }
	public int DepartmentId { get; set; }
	public int DesignationId { get; set; }
	public int? UserId { get; set; }
	public DateOnly DateOfJoining { get; set; }
	public DateOnly? DateOfLeaving { get; set; }
	public DateOnly? DateOfBirth { get; set; }
	public string? Gender { get; set; }
	public string? FatherOrHusbandName { get; set; }
	public string? Phone { get; set; }
	public string? Email { get; set; }
	public string? Address { get; set; }
	public string? PAN { get; set; }
	public string? Aadhaar { get; set; }
	public string? PFNumber { get; set; }
	public string? UANNumber { get; set; }
	public string? ESINumber { get; set; }
	public string? BankName { get; set; }
	public string? BankAccountNumber { get; set; }
	public string? IFSC { get; set; }
	public string? PaymentMode { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
