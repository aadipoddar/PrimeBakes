using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Settings;

public class MaintenanceEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(MaintenanceEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(MaintenanceData.RebuildIndexes), MaintenanceData.RebuildIndexes);
	}
}
