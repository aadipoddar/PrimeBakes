using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class EmployeeData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(EmployeeData));

	public static async Task DeleteTransaction(EmployeeModel employee, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), employee, new { userId, platform });

	public static async Task RecoverTransaction(EmployeeModel employee, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), employee, new { userId, platform });

	public static async Task<int> SaveTransaction(EmployeeModel employee, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), employee, new { userId, platform });
}
