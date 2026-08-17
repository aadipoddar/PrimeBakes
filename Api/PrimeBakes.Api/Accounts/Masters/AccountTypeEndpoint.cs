using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class AccountTypeEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AccountTypeEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(AccountTypeData.DeleteTransaction), AccountTypeData.DeleteTransaction);
		group.MapPost(nameof(AccountTypeData.RecoverTransaction), AccountTypeData.RecoverTransaction);
		group.MapPost(nameof(AccountTypeData.SaveTransaction), AccountTypeData.SaveTransaction);
	}
}
