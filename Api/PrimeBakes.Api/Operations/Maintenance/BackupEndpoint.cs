using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Maintenance;

public class BackupEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BackupEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(BackupData.Backup), BackupData.Backup);
	}
}
