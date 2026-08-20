using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Models.Payroll.PayrollRun;

public sealed record PayrollSaveRequest(
	PayrollModel Payroll,
	List<PayrollDetailModel> PayrollDetails);

public sealed record PayslipBundle(
	PayrollOverviewModel Payroll,
	List<PayrollItemOverviewModel> Components,
	CompanyModel Company,
	EmployeeModel Employee,
	DateTime CurrentDateTime);
