using PrimeBakesLibrary.Notification.Models;

namespace PrimeBakes.Services;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}