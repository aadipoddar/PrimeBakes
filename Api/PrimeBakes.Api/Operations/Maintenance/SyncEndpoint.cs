using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.Maintenance;

public class SyncEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SyncEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(SyncData.Backup), SyncData.Backup);
		group.MapPost(nameof(SyncData.SyncToLocalClient), SyncData.SyncToLocalClient);
		group.MapGet(nameof(SyncData.LoadLastBackupDate), SyncData.LoadLastBackupDate);
	}
}
