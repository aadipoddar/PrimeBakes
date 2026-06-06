using PrimeBakesLibrary.Utils.Notification;

namespace PrimeBakes.Services;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}