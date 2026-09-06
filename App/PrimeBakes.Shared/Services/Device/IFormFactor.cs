namespace PrimeBakes.Shared.Services.Device;

public interface IFormFactor
{
    public string GetFormFactor();
    public string GetPlatform();
    public string GetMachineId();
    public string GetMachineName();
}
