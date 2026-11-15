using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Data.Operations.AuditTrail;

public static class AuditTrailData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AuditTrailData));

	public static async Task SaveAuditTrail(AuditTrailModel auditTrail) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveAuditTrail)), auditTrail);

	public static async Task<List<AuditTrailModel>> LoadAuditTrailByActionUserIdDate(AuditTrailActionTypes Action, int UserId, DateTime StartDate, DateTime EndDate) =>
		await ApiClient.Get<List<AuditTrailModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadAuditTrailByActionUserIdDate)), new { Action, UserId, StartDate, EndDate });

	public static async Task<int> DeleteAuditTrailByActionDate(string? Action, DateTime StartDate, DateTime EndDate, bool saveAuditTrail, int? userId = null, string? formFactor = null, string? platform = null, decimal? latitude = null, decimal? longitude = null) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteAuditTrailByActionDate)), new { },
			new { Action, StartDate, EndDate, saveAuditTrail, userId, formFactor, platform, latitude, longitude });
}
