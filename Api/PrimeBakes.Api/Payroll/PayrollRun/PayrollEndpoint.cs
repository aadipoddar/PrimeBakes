using PrimeBakes.Data.Payroll.PayrollRun;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.PayrollRun;

namespace PrimeBakes.Api.Payroll.PayrollRun;

public class PayrollEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(PayrollEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(PayrollData.LoadPayrollOverviewByEmployeeMonthYear),
			(int? EmployeeId, int? PayrollMonth, int? PayrollYear) => PayrollData.LoadPayrollOverviewByEmployeeMonthYear(EmployeeId, PayrollMonth, PayrollYear));

		group.MapGet(nameof(PayrollData.LoadPayslipBundle),
			(int payrollId) => PayrollData.LoadPayslipBundle(payrollId));

		group.MapGet(nameof(PayrollData.CalculatePayroll),
			(int employeeId, int payrollMonth, int payrollYear) => PayrollData.CalculatePayroll(employeeId, payrollMonth, payrollYear));

		group.MapPost(nameof(PayrollData.SaveTransaction),
			(PayrollSaveRequest request, int userId, string platform) =>
				PayrollData.SaveTransaction(request.Payroll, request.PayrollDetails, userId, platform));

		group.MapPost(nameof(PayrollData.RunPayroll),
			(int payrollMonth, int payrollYear, int userId, string platform) => PayrollData.RunPayroll(payrollMonth, payrollYear, userId, platform));

		group.MapPost(nameof(PayrollData.DeleteTransaction), PayrollData.DeleteTransaction);
		group.MapPost(nameof(PayrollData.RecoverTransaction), PayrollData.RecoverTransaction);
	}
}
