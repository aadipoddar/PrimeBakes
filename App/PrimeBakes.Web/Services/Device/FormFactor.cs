using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Web.Services.Device;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() =>
        "Web";

    public string GetPlatform() =>
        Environment.OSVersion.ToString();

    public string GetMachineId() =>
        null;

    public string GetMachineName() =>
        null;
}
