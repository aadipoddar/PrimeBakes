using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Wasm.Services.Device;

public class FormFactor : IFormFactor
{
	public string GetFormFactor() =>
		"Wasm";

	public string GetPlatform() =>
		Environment.OSVersion.ToString();
}
