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

		group.MapPost(nameof(AuditTrailData.DeleteAuditTrailByDate),
			(DateTime StartDate, DateTime EndDate, int userId, string platform) =>
				AuditTrailData.DeleteAuditTrailByDate(StartDate, EndDate, userId, platform));
	}
}
