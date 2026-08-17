using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class CompanyEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CompanyEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CompanyData.DeleteTransaction), CompanyData.DeleteTransaction);
		group.MapPost(nameof(CompanyData.RecoverTransaction), CompanyData.RecoverTransaction);
		group.MapPost(nameof(CompanyData.SaveTransaction), CompanyData.SaveTransaction);
	}
}
