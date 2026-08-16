using PrimeBakes.Shared.Services;

namespace PrimeBakes.Wasm.Services;

public class NotificationService : INotificationService
{
	public Task RegisterDevicePushNotification(string tag) =>
		Task.CompletedTask;

	public Task DeregisterDevicePushNotification() =>
		Task.CompletedTask;

	public Task ShowLocalNotification(int id, string title, string subTitle, string description) =>
		Task.CompletedTask;
}
