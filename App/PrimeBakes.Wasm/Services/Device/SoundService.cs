using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Wasm.Services.Device;

public class SoundService(IJSRuntime jsRuntime) : ISoundService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task PlaySound(string soundFileName) =>
		await _jsRuntime.InvokeVoidAsync("PlaySound", soundFileName);
}
