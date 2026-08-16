using PrimeBakes.Shared.Components.Dialog;

namespace PrimeBakes.Shared.Layout;

/// <summary>
/// Main layout wrapping all pages. Handles Bluetooth printer auto-reconnect
/// on every full page reload so the printer stays connected across navigations.
/// </summary>
public partial class MainLayout
{
	private ToastNotification _toastNotification;

	private string _savedPrinterName = string.Empty;
	private string _savedPrinterAddress = string.Empty;

	/// <summary>
	/// Persists the saved printer record with <c>IsConnected = true</c> after a successful reconnect.
	/// </summary>
	private async Task MarkSavedPrinterConnectedAsync()
	{
		if (string.IsNullOrWhiteSpace(_savedPrinterAddress))
			return;

		var info = new BluetoothDeviceInfo
		{
			Name = BluetoothPrinterService.ConnectedPrinterName ?? _savedPrinterName,
			Address = _savedPrinterAddress,
			IsPaired = true,
			IsConnected = true
		};
		var json = System.Text.Json.JsonSerializer.Serialize(info);
		await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, json);
	}

	/// <summary>
	/// Persists the saved printer record with <c>IsConnected = false</c> so the device
	/// is remembered for future reconnect attempts without treating it as connected.
	/// </summary>
	private async Task MarkSavedPrinterDisconnectedAsync()
	{
		if (string.IsNullOrWhiteSpace(_savedPrinterAddress))
			return;

		var info = new BluetoothDeviceInfo
		{
			Name = _savedPrinterName,
			Address = _savedPrinterAddress,
			IsPaired = true,
			IsConnected = false
		};
		var json = System.Text.Json.JsonSerializer.Serialize(info);
		await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, json);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await ReconnectSavedPrinterAsync();
	}

	private async Task ReconnectSavedPrinterAsync()
	{
		try
		{
			if (!BluetoothPrinterService.IsSupported || BluetoothPrinterService.IsConnected)
				return;

			var json = await DataStorageService.LocalGetAsync(StorageFileNames.BluetoothPrinterDataFileName);
			if (string.IsNullOrWhiteSpace(json))
				return;

			var saved = System.Text.Json.JsonSerializer.Deserialize<BluetoothDeviceInfo>(json);
			if (saved is null || string.IsNullOrWhiteSpace(saved.Address))
			{
				await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);
				return;
			}

			// Cache for the helper so all code paths can mark the printer disconnected
			_savedPrinterName = saved.Name ?? "Unknown";
			_savedPrinterAddress = saved.Address;

			var connected = await BluetoothPrinterService.ConnectAsync(saved.Address);

			if (connected)
			{
				await MarkSavedPrinterConnectedAsync();
				await _toastNotification.ShowAsync(
					"Printer Connected",
					$"Reconnected to {BluetoothPrinterService.ConnectedPrinterName}.",
					ToastType.Success);
			}
			else
			{
				await MarkSavedPrinterDisconnectedAsync();
				await _toastNotification.ShowAsync(
					"Printer Unavailable",
					$"Could not reconnect to {saved.Name}. Please reconnect from Local Settings.",
					ToastType.Warning);
			}
		}
		catch
		{
			await MarkSavedPrinterDisconnectedAsync();
		}
	}
}
