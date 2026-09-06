using PrimeBakes.Shared.Services.Device;

#if WINDOWS
using Microsoft.Win32;
#endif

namespace PrimeBakes.Services.Device;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() =>
        DeviceInfo.Idiom.ToString();

    public string GetPlatform() =>
        DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;

    public string GetMachineId()
    {
#if WINDOWS
        using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var cryptography = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return cryptography?.GetValue("MachineGuid")?.ToString();
#else
        return null;
#endif
    }

    public string GetMachineName() =>
#if WINDOWS
        Environment.MachineName;
#else
        null;
#endif
}
