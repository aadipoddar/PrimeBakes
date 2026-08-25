using PrimeBakes.Api.Common;
using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Api.Payroll.Masters;

public class EmployeeSalaryComponentEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(EmployeeSalaryComponentEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapPost(nameof(EmployeeSalaryComponentData.InsertEmployeeSalaryComponent),
			(EmployeeSalaryComponentModel employeeSalaryComponent) => EmployeeSalaryComponentData.InsertEmployeeSalaryComponent(employeeSalaryComponent));

		group.MapPost(nameof(EmployeeSalaryComponentData.DeleteEmployeeSalaryComponentById),
			(int id) => EmployeeSalaryComponentData.DeleteEmployeeSalaryComponentById(id));

		group.MapGet(nameof(EmployeeSalaryComponentData.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate),
			(int? EmployeeId, int? SalaryComponentId, DateOnly? Date) => EmployeeSalaryComponentData.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(EmployeeId, SalaryComponentId, Date));

		group.MapGet(nameof(EmployeeSalaryComponentData.LoadEffectiveSalaryComponents),
			(int employeeId, DateOnly asOn) => EmployeeSalaryComponentData.LoadEffectiveSalaryComponents(employeeId, asOn));

		group.MapPost(nameof(EmployeeSalaryComponentData.DeleteTransaction), EmployeeSalaryComponentData.DeleteTransaction);
		group.MapPost(nameof(EmployeeSalaryComponentData.DiscontinueTransaction), EmployeeSalaryComponentData.DiscontinueTransaction);
		group.MapPost(nameof(EmployeeSalaryComponentData.SaveTransaction), EmployeeSalaryComponentData.SaveTransaction);
	}
}
