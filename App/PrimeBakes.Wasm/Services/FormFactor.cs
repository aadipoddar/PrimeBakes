using PrimeBakes.Shared.Services;

namespace PrimeBakes.Wasm.Services;

public class FormFactor : IFormFactor
{
	public string GetFormFactor() =>
		"Wasm";

	public string GetPlatform() =>
		Environment.OSVersion.ToString();
}
