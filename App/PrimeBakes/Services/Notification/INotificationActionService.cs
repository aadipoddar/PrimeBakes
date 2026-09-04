namespace PrimeBakes.Services.Notification;

public interface INotificationActionService
{
    void TriggerAction(string action);
}