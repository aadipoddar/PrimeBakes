using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class StateUTEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(StateUTEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(StateUTData.DeleteTransaction), StateUTData.DeleteTransaction);
		group.MapPost(nameof(StateUTData.RecoverTransaction), StateUTData.RecoverTransaction);
		group.MapPost(nameof(StateUTData.SaveTransaction), StateUTData.SaveTransaction);
	}
}
