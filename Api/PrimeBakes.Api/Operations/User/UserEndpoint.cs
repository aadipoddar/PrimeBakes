using PrimeBakes.Data.Operations.User;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.User;

public class UserEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(UserEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(UserData.LoadUserByPasscode),
			(int Passcode) => UserData.LoadUserByPasscode(Passcode));

		group.MapPost(nameof(UserData.UpdateLastLoginTime), UserData.UpdateLastLoginTime);
		group.MapPost(nameof(UserData.DeleteTransaction), UserData.DeleteTransaction);
		group.MapPost(nameof(UserData.RecoverTransaction), UserData.RecoverTransaction);
		group.MapPost(nameof(UserData.SaveTransaction), UserData.SaveTransaction);
	}
}
