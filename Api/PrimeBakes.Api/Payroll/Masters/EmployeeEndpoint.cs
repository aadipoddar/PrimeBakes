using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Payroll.Masters;

public class EmployeeEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(EmployeeEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(EmployeeData.DeleteTransaction), EmployeeData.DeleteTransaction);
		group.MapPost(nameof(EmployeeData.RecoverTransaction), EmployeeData.RecoverTransaction);
		group.MapPost(nameof(EmployeeData.SaveTransaction), EmployeeData.SaveTransaction);
	}
}
