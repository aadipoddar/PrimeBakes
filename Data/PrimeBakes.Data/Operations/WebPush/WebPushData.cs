using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.WebPush;

namespace PrimeBakes.Data.Operations.WebPush;

public static class WebPushData
{
	public static async Task SaveWebPushSubscription(WebPushSubscriptionModel subscription) =>
		await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertWebPushSubscription, subscription);

	public static async Task<List<WebPushSubscriptionModel>> LoadWebPushSubscriptionByUserId(int UserId) =>
		await SqlDataAccess.LoadData<WebPushSubscriptionModel, dynamic>(OperationNames.LoadWebPushSubscriptionByUserId, new { UserId });

	public static async Task DeleteWebPushSubscriptionByEndpoint(string Endpoint) =>
		await SqlDataAccess.LoadData<int, dynamic>(OperationNames.DeleteWebPushSubscriptionByEndpoint, new { Endpoint });
}
