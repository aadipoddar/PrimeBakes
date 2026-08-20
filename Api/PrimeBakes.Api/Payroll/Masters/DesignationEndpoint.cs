using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Payroll.Masters;

public class DesignationEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DesignationEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DesignationData.DeleteTransaction), DesignationData.DeleteTransaction);
		group.MapPost(nameof(DesignationData.RecoverTransaction), DesignationData.RecoverTransaction);
		group.MapPost(nameof(DesignationData.SaveTransaction), DesignationData.SaveTransaction);
	}
}
