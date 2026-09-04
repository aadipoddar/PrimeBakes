namespace PrimeBakes.Shared.Services.Notification;

public interface INotificationService
{
    public Task RegisterDevicePushNotification(string tag);
    public Task DeregisterDevicePushNotification();

    public Task ShowLocalNotification(int id, string title, string subTitle, string description);
}
