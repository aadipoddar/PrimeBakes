using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class DesignationData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DesignationData));

	public static async Task DeleteTransaction(DesignationModel designation, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), designation, new { userId, formFactor, platform, latitude, longitude });

	public static async Task RecoverTransaction(DesignationModel designation, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), designation, new { userId, formFactor, platform, latitude, longitude });

	public static async Task<int> SaveTransaction(DesignationModel designation, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), designation, new { userId, formFactor, platform, latitude, longitude });
}
