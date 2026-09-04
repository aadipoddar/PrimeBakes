using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Services.Device;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() =>
        DeviceInfo.Idiom.ToString();

    public string GetPlatform() =>
        DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
}
