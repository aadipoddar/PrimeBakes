using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Data.Operations.AuditTrail;

public static class AuditTrailData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AuditTrailData));

	public static async Task SaveAuditTrail(AuditTrailModel auditTrail) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveAuditTrail)), auditTrail);

	public static async Task<int> DeleteAuditTrailByDate(DateTime StartDate, DateTime EndDate, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteAuditTrailByDate)), new { },
			new { StartDate, EndDate, userId, formFactor, platform, latitude, longitude });
}
