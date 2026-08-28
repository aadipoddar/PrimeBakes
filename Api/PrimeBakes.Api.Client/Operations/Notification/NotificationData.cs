using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Notification;

namespace PrimeBakes.Data.Operations.Notification;

public static class NotificationData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(NotificationData));

	public static async Task SendCustomNotification(List<int> userIds, string title, string text, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SendCustomNotification)),
			new SendCustomNotificationRequest(userIds, title, text),
			new { userId, formFactor, platform, latitude, longitude });
}
