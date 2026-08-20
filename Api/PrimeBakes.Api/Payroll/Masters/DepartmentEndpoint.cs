using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Payroll.Masters;

public class DepartmentEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DepartmentEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DepartmentData.DeleteTransaction), DepartmentData.DeleteTransaction);
		group.MapPost(nameof(DepartmentData.RecoverTransaction), DepartmentData.RecoverTransaction);
		group.MapPost(nameof(DepartmentData.SaveTransaction), DepartmentData.SaveTransaction);
	}
}
