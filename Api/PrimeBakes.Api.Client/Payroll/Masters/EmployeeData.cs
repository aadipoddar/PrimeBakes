using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class EmployeeData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(EmployeeData));

	public static async Task DeleteTransaction(EmployeeModel employee, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), employee, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(EmployeeModel employee, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), employee, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(EmployeeModel employee, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), employee, new { userId, formFactor, platform, latitude, longitude });
}
