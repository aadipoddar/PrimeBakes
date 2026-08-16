using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class CompanyDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CompanyDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CompanyData.DeleteTransaction), CompanyData.DeleteTransaction);
		group.MapPost(nameof(CompanyData.RecoverTransaction), CompanyData.RecoverTransaction);
		group.MapPost(nameof(CompanyData.SaveTransaction), CompanyData.SaveTransaction);
	}
}
