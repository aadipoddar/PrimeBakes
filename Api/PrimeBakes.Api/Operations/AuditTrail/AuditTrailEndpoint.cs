using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Api.Operations.AuditTrail;

public class AuditTrailEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AuditTrailEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(AuditTrailData.SaveAuditTrail),
			(AuditTrailModel auditTrail) => AuditTrailData.SaveAuditTrail(auditTrail));

		group.MapGet(nameof(AuditTrailData.LoadAuditTrailByActionUserIdDate),
			(AuditTrailActionTypes Action, int UserId, DateTime StartDate, DateTime EndDate) =>
				AuditTrailData.LoadAuditTrailByActionUserIdDate(Action, UserId, StartDate, EndDate));

		group.MapPost(nameof(AuditTrailData.DeleteAuditTrailByActionDate),
			(string? Action, DateTime StartDate, DateTime EndDate, bool saveAuditTrail, int? userId = null, string? formFactor = null, string? platform = null, decimal? latitude = null, decimal? longitude = null) =>
				AuditTrailData.DeleteAuditTrailByActionDate(Action, StartDate, EndDate, saveAuditTrail, userId, formFactor, platform, latitude, longitude));
	}
}
