using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class EmployeeSalaryComponentData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(EmployeeSalaryComponentData));

	public static async Task<int> InsertEmployeeSalaryComponent(EmployeeSalaryComponentModel employeeSalaryComponent) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(InsertEmployeeSalaryComponent)), employeeSalaryComponent);

	public static async Task<int> DeleteEmployeeSalaryComponentById(int id) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteEmployeeSalaryComponentById)), new { }, new { id });

	public static async Task<List<EmployeeSalaryComponentOverviewModel>> LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(int? EmployeeId = null, int? SalaryComponentId = null, DateOnly? Date = null) =>
		await ApiClient.Get<List<EmployeeSalaryComponentOverviewModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate)), new { EmployeeId, SalaryComponentId, Date });

	public static async Task<List<SalaryComponentModel>> LoadEffectiveSalaryComponents(int employeeId, DateOnly asOn) =>
		await ApiClient.Get<List<SalaryComponentModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadEffectiveSalaryComponents)), new { employeeId, asOn });

	public static async Task DeleteTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), employeeSalaryComponent, new { userId, platform });

	public static async Task DiscontinueTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DiscontinueTransaction)), employeeSalaryComponent, new { userId, platform });

	public static async Task<int> SaveTransaction(EmployeeSalaryComponentModel employeeSalaryComponent, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), employeeSalaryComponent, new { userId, platform });
}
