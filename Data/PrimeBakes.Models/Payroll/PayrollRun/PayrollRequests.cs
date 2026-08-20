namespace PrimeBakes.Models.Payroll.PayrollRun;

public sealed record PayrollSaveRequest(
	PayrollModel Payroll,
	List<PayrollDetailModel> PayrollDetails);
