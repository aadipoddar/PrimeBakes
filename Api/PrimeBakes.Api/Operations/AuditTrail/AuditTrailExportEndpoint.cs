using PrimeBakes.Library.Operations.AuditTrail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Api.Operations.AuditTrail;

public class AuditTrailExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AuditTrailExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(AuditTrailExport.ExportReport), async (AuditTrailReportRequest request) =>
		{
			var (stream, fileName) = await AuditTrailExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd, request.ShowAllColumns);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}
