using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Payroll.Masters;

public class SalaryComponentEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SalaryComponentEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(SalaryComponentData.DeleteTransaction), SalaryComponentData.DeleteTransaction);
		group.MapPost(nameof(SalaryComponentData.RecoverTransaction), SalaryComponentData.RecoverTransaction);
		group.MapPost(nameof(SalaryComponentData.SaveTransaction), SalaryComponentData.SaveTransaction);
	}
}
