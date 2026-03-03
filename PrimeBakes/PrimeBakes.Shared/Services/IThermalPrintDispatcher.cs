using Microsoft.JSInterop;

namespace PrimeBakes.Shared.Services;

/// <summary>
/// Dispatches a thermal print job to the correct output:
/// - Bluetooth is connected → sends ESC/POS raster bytes directly to the printer.
/// - Bluetooth is not connected → PNG-encodes the receipt and opens the browser print dialog.
/// Centralises the if/else dispatch so callers only supply the two generator delegates.
/// </summary>
public interface IThermalPrintDispatcher
{
	/// <summary>
	/// Prints the receipt via Bluetooth if connected, or via the browser print dialog otherwise.
	/// </summary>
	/// <param name="generateEscPos">Async factory that produces ESC/POS byte array.</param>
	/// <param name="generatePng">Async factory that produces PNG byte array (browser fallback).</param>
	Task PrintAsync(Func<Task<byte[]>> generateEscPos, Func<Task<byte[]>> generatePng);
}

/// <summary>
/// Default implementation of <see cref="IThermalPrintDispatcher"/>.
/// Register as Scoped so the correct <see cref="IJSRuntime"/> circuit is always used.
/// </summary>
public class ThermalPrintDispatcher(IBluetoothPrinterService bluetoothPrinterService, IJSRuntime jsRuntime)
	: IThermalPrintDispatcher
{
	public async Task PrintAsync(Func<Task<byte[]>> generateEscPos, Func<Task<byte[]>> generatePng)
	{
		if (bluetoothPrinterService.IsConnected)
		{
			var data = await generateEscPos();
			if (data.Length > 0)
				await bluetoothPrinterService.SendDataAsync(data);
		}
		else
		{
			var png = await generatePng();
			if (png.Length > 0)
				await jsRuntime.InvokeVoidAsync("printThermalImage", Convert.ToBase64String(png));
		}
	}
}
