using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class SalaryComponentData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SalaryComponentData));

	public static async Task DeleteTransaction(SalaryComponentModel salaryComponent, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), salaryComponent, new { userId, platform });

	public static async Task RecoverTransaction(SalaryComponentModel salaryComponent, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), salaryComponent, new { userId, platform });

	public static async Task<int> SaveTransaction(SalaryComponentModel salaryComponent, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), salaryComponent, new { userId, platform });
}
