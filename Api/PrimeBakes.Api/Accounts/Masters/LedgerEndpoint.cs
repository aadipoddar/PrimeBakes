using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class LedgerEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(LedgerEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(LedgerData.DeleteTransaction), LedgerData.DeleteTransaction);
		group.MapPost(nameof(LedgerData.RecoverTransaction), LedgerData.RecoverTransaction);
		group.MapPost(nameof(LedgerData.SaveTransaction), LedgerData.SaveTransaction);
	}
}
