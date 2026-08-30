using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Maintenance;

public class MaintenanceEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(MaintenanceEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(MaintenanceData.RebuildIndexes), MaintenanceData.RebuildIndexes);
		group.MapGet(nameof(MaintenanceData.LoadDatabaseSize), MaintenanceData.LoadDatabaseSize);
	}
}
