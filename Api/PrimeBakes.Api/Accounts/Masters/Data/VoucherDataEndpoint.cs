using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class VoucherDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(VoucherDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(VoucherData.DeleteTransaction), VoucherData.DeleteTransaction);
		group.MapPost(nameof(VoucherData.RecoverTransaction), VoucherData.RecoverTransaction);
		group.MapPost(nameof(VoucherData.SaveTransaction), VoucherData.SaveTransaction);
	}
}
