using PrimeBakes.Models.Operations.Notification;

namespace PrimeBakes.Services.Notification;

public interface IDeviceInstallationService
{
    string Token { get; set; }
    bool NotificationsSupported { get; }
    string GetDeviceId();
    DeviceInstallation GetDeviceInstallation(params string[] tags);
}