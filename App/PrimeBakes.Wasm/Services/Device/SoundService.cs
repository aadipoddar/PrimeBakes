using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Wasm.Services;

public class SoundService(IJSRuntime jsRuntime) : ISoundService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task PlaySound(string soundFileName) =>
		await _jsRuntime.InvokeVoidAsync("PlaySound", soundFileName);
}
