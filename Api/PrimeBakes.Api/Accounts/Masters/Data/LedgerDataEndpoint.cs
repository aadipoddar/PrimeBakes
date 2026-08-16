using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class LedgerDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(LedgerDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(LedgerData.DeleteTransaction), LedgerData.DeleteTransaction);
		group.MapPost(nameof(LedgerData.RecoverTransaction), LedgerData.RecoverTransaction);
		group.MapPost(nameof(LedgerData.SaveTransaction), LedgerData.SaveTransaction);
	}
}
