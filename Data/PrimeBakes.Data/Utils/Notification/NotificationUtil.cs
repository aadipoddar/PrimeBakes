using PrimeBakes.Data.Operations.WebPush;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.User;

using System.Net;
using System.Text.Json;

using WebPush;

namespace PrimeBakes.Data.Utils.Notification;

internal static class NotificationUtil
{
	internal static async Task SendNotificationToAPI(List<UserModel> users, string title, string text)
	{
		if (SqlDataAccess._databaseConnection != Secrets.AzureConnectionString)
			return;

		string endpoint = $"{CommonSecrets.NotificationBackendServiceEndpoint}api/notifications/requests";
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("apikey", CommonSecrets.NotificationAPIKey);

		var notificationPayload = new
		{
			Title = title,
			Text = text,
			Action = "action_a",
			Tags = users.Select(u => u.Id.ToString()).ToArray(),
		};

		string jsonPayload = JsonSerializer.Serialize(notificationPayload, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		});

		var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
		var response = await httpClient.PostAsync(endpoint, content);

		await SendWebPushNotification(users, title, text);
	}

	private static async Task SendWebPushNotification(List<UserModel> users, string title, string text)
	{
		if (string.IsNullOrWhiteSpace(CommonSecrets.WebPushPublicKey) || string.IsNullOrWhiteSpace(Secrets.WebPushPrivateKey))
			return;

		var vapidDetails = new VapidDetails($"mailto:{Secrets.Email}", CommonSecrets.WebPushPublicKey, Secrets.WebPushPrivateKey);
		var webPushClient = new WebPushClient();
		var payload = JsonSerializer.Serialize(new { title, body = text, url = "/" });

		foreach (var user in users)
			foreach (var subscription in await WebPushData.LoadWebPushSubscriptionByUserId(user.Id))
				try
				{
					await webPushClient.SendNotificationAsync(new PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth), payload, vapidDetails);
				}
				catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
				{
					await WebPushData.DeleteWebPushSubscriptionByEndpoint(subscription.Endpoint);
				}
				catch { }
	}

	internal class TransactionNotificationData
	{
		public string TransactionType { get; set; } // "Order", "Sale", "Purchase", etc.
		public string TransactionNo { get; set; }
		public NotifyType Action { get; set; }
		public string LocationName { get; set; }
		public Dictionary<string, string> Details { get; set; } // Key-value pairs for notification details
		public string Remarks { get; set; }
	}

	internal static async Task SendTransactionNotification(List<UserModel> users, TransactionNotificationData data)
	{
		// Enhanced notification with emoji and better formatting
		var (actionEmoji, actionText) = data.Action switch
		{
			NotifyType.Updated => ("✏️", "Updated"),
			NotifyType.Deleted => ("🗑️", "Deleted"),
			NotifyType.Recovered => ("♻️", "Recovered"),
			_ => ("✅", "Created")
		};

		var title = $"{actionEmoji} {data.TransactionType} {actionText} | {data.LocationName}";

		// Structured notification body with better formatting
		var detailsText = string.Join("\n", data.Details.Select(d => $"{d.Key}: {d.Value}"));
		var remarksText = string.IsNullOrWhiteSpace(data.Remarks) ? "" : $"\n💬 {data.Remarks}";

		var text = $@"{data.TransactionType} #{data.TransactionNo}

{detailsText}{remarksText}";

		await SendNotificationToAPI(users, title, text);
	}
}