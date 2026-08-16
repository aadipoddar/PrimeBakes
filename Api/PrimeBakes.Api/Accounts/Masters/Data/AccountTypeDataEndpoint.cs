using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class AccountTypeDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AccountTypeDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(AccountTypeData.DeleteTransaction), AccountTypeData.DeleteTransaction);
		group.MapPost(nameof(AccountTypeData.RecoverTransaction), AccountTypeData.RecoverTransaction);
		group.MapPost(nameof(AccountTypeData.SaveTransaction), AccountTypeData.SaveTransaction);
	}
}
