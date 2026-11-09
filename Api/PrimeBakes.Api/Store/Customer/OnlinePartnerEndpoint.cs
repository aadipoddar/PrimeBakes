using PrimeBakes.Api.Common;
using PrimeBakes.Data.Store.Customer;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Customer;

public class OnlinePartnerEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(OnlinePartnerEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapPost(nameof(OnlinePartnerData.DeleteTransaction), OnlinePartnerData.DeleteTransaction);
		group.MapPost(nameof(OnlinePartnerData.RecoverTransaction), OnlinePartnerData.RecoverTransaction);
		group.MapPost(nameof(OnlinePartnerData.SaveTransaction), OnlinePartnerData.SaveTransaction);
	}
}
