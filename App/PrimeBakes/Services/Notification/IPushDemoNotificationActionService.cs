using PrimeBakes.Models.Operations.Notification;

namespace PrimeBakes.Services.Notification;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}