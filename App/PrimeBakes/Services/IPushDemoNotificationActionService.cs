using PrimeBakes.Library.Utils.Notification;

namespace PrimeBakes.Services;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}