using Microsoft.JSInterop;

using PrimeBakes.Data.Operations.WebPush;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.WebPush;

namespace PrimeBakes.Shared.Services.Notification;

public class BrowserNotificationService(IJSRuntime jsRuntime) : INotificationService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task RegisterDevicePushNotification(string tag)
	{
		if (!int.TryParse(tag, out var userId))
			return;

		try
		{
			var subscription = await _jsRuntime.InvokeAsync<WebPushSubscriptionModel>("requestPushSubscription", CommonSecrets.WebPushPublicKey);

			if (string.IsNullOrWhiteSpace(subscription?.Endpoint))
				return;

			subscription.UserId = userId;
			await WebPushData.SaveWebPushSubscription(subscription);
		}
		catch { }
	}

	public async Task DeregisterDevicePushNotification()
	{
		try
		{
			var endpoint = await _jsRuntime.InvokeAsync<string>("removePushSubscription");

			if (!string.IsNullOrWhiteSpace(endpoint))
				await WebPushData.DeleteWebPushSubscriptionByEndpoint(endpoint);
		}
		catch { }
	}

	public async Task ShowLocalNotification(int id, string title, string subTitle, string description)
	{
		try { await _jsRuntime.InvokeVoidAsync("showLocalNotification", title, $"{subTitle}\n{description}"); }
		catch { }
	}
}
