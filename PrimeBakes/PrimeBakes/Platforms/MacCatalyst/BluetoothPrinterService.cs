using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

/// <summary>
/// macCatalyst fallback implementation for Bluetooth printer service.
/// Bluetooth Classic (RFCOMM/SPP) is not available via public Apple APIs on macOS Catalyst.
/// </summary>
public class BluetoothPrinterService : IBluetoothPrinterService
{
	/// <inheritdoc />
	public bool IsSupported => false;

	/// <inheritdoc />
	public bool IsEnabled => false;

	/// <inheritdoc />
	public bool IsConnected => false;

	/// <inheritdoc />
	public string ConnectedPrinterName => string.Empty;

	/// <inheritdoc />
	public string ConnectedPrinterAddress => string.Empty;

	/// <inheritdoc />
	public Task<List<BluetoothDeviceInfo>> DiscoverDevicesAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(new List<BluetoothDeviceInfo>());

	/// <inheritdoc />
	public Task<bool> ConnectAsync(string address) =>
		Task.FromResult(false);

	/// <inheritdoc />
	public Task DisconnectAsync() =>
		Task.CompletedTask;

	/// <inheritdoc />
	public Task<bool> SendDataAsync(byte[] data) =>
		Task.FromResult(false);

	/// <inheritdoc />
	public Task<bool> PrintTextAsync(string text) =>
		Task.FromResult(false);

	/// <inheritdoc />
	public Task<bool> RequestPermissionsAsync() =>
		Task.FromResult(false);
}
