using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class DepartmentData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DepartmentData));

	public static async Task DeleteTransaction(DepartmentModel department, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), department, new { userId, platform });

	public static async Task RecoverTransaction(DepartmentModel department, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), department, new { userId, platform });

	public static async Task<int> SaveTransaction(DepartmentModel department, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), department, new { userId, platform });
}
