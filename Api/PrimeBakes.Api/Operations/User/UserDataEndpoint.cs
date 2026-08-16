using PrimeBakes.Library.Operations.User;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Operations.User;

public class UserDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(UserDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(UserData.LoadUserByPasscode),
			(int Passcode) => UserData.LoadUserByPasscode(Passcode));

		group.MapPost(nameof(UserData.DeleteTransaction), UserData.DeleteTransaction);
		group.MapPost(nameof(UserData.RecoverTransaction), UserData.RecoverTransaction);
		group.MapPost(nameof(UserData.SaveTransaction), UserData.SaveTransaction);
	}
}
