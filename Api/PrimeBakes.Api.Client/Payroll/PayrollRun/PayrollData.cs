using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.PayrollRun;

namespace PrimeBakes.Data.Payroll.PayrollRun;

public static class PayrollData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PayrollData));

	public static async Task<List<PayrollOverviewModel>> LoadPayrollOverviewByEmployeeMonthYear(int? EmployeeId = null, int? PayrollMonth = null, int? PayrollYear = null) =>
		await ApiClient.Get<List<PayrollOverviewModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadPayrollOverviewByEmployeeMonthYear)), new { EmployeeId, PayrollMonth, PayrollYear });

	public static async Task<PayslipBundle> LoadPayslipBundle(int payrollId) =>
		await ApiClient.Get<PayslipBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadPayslipBundle)), new { payrollId });

	public static async Task<PayrollSaveRequest> CalculatePayroll(int employeeId, int payrollMonth, int payrollYear) =>
		await ApiClient.Get<PayrollSaveRequest>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(CalculatePayroll)), new { employeeId, payrollMonth, payrollYear });

	public static async Task<int> SaveTransaction(PayrollModel payroll, List<PayrollDetailModel> payrollDetails, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), new PayrollSaveRequest(payroll, payrollDetails), new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> RunPayroll(int payrollMonth, int payrollYear, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RunPayroll)), null, new { payrollMonth, payrollYear, userId, formFactor, platform, latitude, longitude });

	public static async Task DeleteTransaction(PayrollModel payroll, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), payroll, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(PayrollModel payroll, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), payroll, new { userId, formFactor, platform, latitude, longitude });
}
