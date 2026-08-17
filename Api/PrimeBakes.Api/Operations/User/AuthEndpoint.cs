using PrimeBakes.Data.Operations.User;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.User;

public class AuthEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AuthEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).AllowAnonymous();

		group.MapGet(nameof(AuthData.Login),
			(int Passcode) => AuthData.Login(Passcode));
	}
}
