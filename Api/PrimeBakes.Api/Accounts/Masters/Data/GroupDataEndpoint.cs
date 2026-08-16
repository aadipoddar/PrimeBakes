using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class GroupDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GroupDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(GroupData.DeleteTransaction), GroupData.DeleteTransaction);
		group.MapPost(nameof(GroupData.RecoverTransaction), GroupData.RecoverTransaction);
		group.MapPost(nameof(GroupData.SaveTransaction), GroupData.SaveTransaction);
	}
}
