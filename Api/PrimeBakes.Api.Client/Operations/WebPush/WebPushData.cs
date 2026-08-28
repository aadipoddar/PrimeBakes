using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.WebPush;

namespace PrimeBakes.Data.Operations.WebPush;

public static class WebPushData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(WebPushData));

	public static async Task SaveWebPushSubscription(WebPushSubscriptionModel subscription) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveWebPushSubscription)), subscription);

	public static async Task DeleteWebPushSubscriptionByEndpoint(string Endpoint) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteWebPushSubscriptionByEndpoint)), new { }, new { Endpoint });
}
