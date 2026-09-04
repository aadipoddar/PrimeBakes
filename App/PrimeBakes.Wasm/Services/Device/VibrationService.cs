using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Wasm.Services.Device;

public class VibrationService(IJSRuntime jsRuntime) : IVibrationService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public void VibrateHapticClick() =>
		Vibrate(10);

	public void VibrateHapticLongPress() =>
		Vibrate(40);

	public void VibrateWithTime(int milliseconds) =>
		Vibrate(milliseconds);

	private void Vibrate(int milliseconds) =>
		_ = InvokeVibrate(milliseconds);

	private async Task InvokeVibrate(int milliseconds)
	{
		try { await _jsRuntime.InvokeVoidAsync("navigator.vibrate", milliseconds); }
		catch { }
	}
}
