using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class GroupEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GroupEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(GroupData.DeleteTransaction), GroupData.DeleteTransaction);
		group.MapPost(nameof(GroupData.RecoverTransaction), GroupData.RecoverTransaction);
		group.MapPost(nameof(GroupData.SaveTransaction), GroupData.SaveTransaction);
	}
}
