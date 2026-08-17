using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters;

public class VoucherEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(VoucherEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(VoucherData.DeleteTransaction), VoucherData.DeleteTransaction);
		group.MapPost(nameof(VoucherData.RecoverTransaction), VoucherData.RecoverTransaction);
		group.MapPost(nameof(VoucherData.SaveTransaction), VoucherData.SaveTransaction);
	}
}
