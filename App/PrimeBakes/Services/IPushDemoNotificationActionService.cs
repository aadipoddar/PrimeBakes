using PrimeBakes.Models.Operations.Notification;

namespace PrimeBakes.Services;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}