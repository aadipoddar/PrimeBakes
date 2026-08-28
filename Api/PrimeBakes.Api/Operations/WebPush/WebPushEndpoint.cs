using PrimeBakes.Data.Operations.WebPush;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.WebPush;

namespace PrimeBakes.Api.Operations.WebPush;

public class WebPushEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(WebPushEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(WebPushData.SaveWebPushSubscription),
			(WebPushSubscriptionModel subscription) => WebPushData.SaveWebPushSubscription(subscription));

		group.MapPost(nameof(WebPushData.DeleteWebPushSubscriptionByEndpoint),
			(string Endpoint) => WebPushData.DeleteWebPushSubscriptionByEndpoint(Endpoint));
	}
}
