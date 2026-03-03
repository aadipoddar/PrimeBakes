using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes.Shared.Layout;

/// <summary>
/// Main layout wrapping all pages. Handles Bluetooth printer auto-reconnect
/// on every full page reload so the printer stays connected across navigations.
/// </summary>
public partial class MainLayout
{
	private BluetoothReconnectDialog _bluetoothReconnectDialog;
	private ToastNotification _toastNotification;

	private string _savedPrinterName = string.Empty;
	private string _savedPrinterAddress = string.Empty;
	private bool _isReconnecting;

	/// <summary>
	/// Persists the saved printer record with <c>IsConnected = true</c> after a successful reconnect.
	/// </summary>
	private async Task MarkSavedPrinterConnectedAsync()
	{
		if (string.IsNullOrEmpty(_savedPrinterAddress))
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
		if (string.IsNullOrEmpty(_savedPrinterAddress))
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

	private string Factor =>
		FormFactor.GetFormFactor();

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
			if (BluetoothPrinterService.IsConnected)
				return;

			var json = await DataStorageService.LocalGetAsync(StorageFileNames.BluetoothPrinterDataFileName);
			if (string.IsNullOrEmpty(json))
				return;

			var saved = System.Text.Json.JsonSerializer.Deserialize<BluetoothDeviceInfo>(json);
			if (saved is null || string.IsNullOrEmpty(saved.Address))
			{
				await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);
				return;
			}

			// Cache for the helper so all code paths can mark the printer disconnected
			_savedPrinterName = saved.Name ?? "Unknown";
			_savedPrinterAddress = saved.Address;

			// On web, requestDevice requires a user gesture — show a reconnect dialog.
			if (Factor == "Web")
			{
				await _bluetoothReconnectDialog.ShowAsync();
				return;
			}

			// On native platforms (Android/Windows), silently reconnect
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

	/// <summary>
	/// Called when the user clicks the reconnect button in the dialog.
	/// This provides the user gesture required by the Web Bluetooth API.
	/// </summary>
	private async Task ReconnectPrinterFromDialog()
	{
		try
		{
			_isReconnecting = true;
			StateHasChanged();

			var connected = await BluetoothPrinterService.ConnectAsync(_savedPrinterAddress);

			if (connected)
			{
				await MarkSavedPrinterConnectedAsync();
				await _bluetoothReconnectDialog.HideAsync();
				await _toastNotification.ShowAsync(
					"Printer Connected",
					$"Reconnected to {BluetoothPrinterService.ConnectedPrinterName}.",
					ToastType.Success);
			}
			else
			{
				await MarkSavedPrinterDisconnectedAsync();
				await _bluetoothReconnectDialog.HideAsync();
				await _toastNotification.ShowAsync(
					"Printer Unavailable",
					$"Could not reconnect to {_savedPrinterName}. Please reconnect from Local Settings.",
					ToastType.Warning);
			}
		}
		catch
		{
			await MarkSavedPrinterDisconnectedAsync();
			await _bluetoothReconnectDialog.HideAsync();
		}
		finally
		{
			_isReconnecting = false;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Dismisses the reconnect dialog without reconnecting.
	/// Marks the printer as disconnected so it is remembered for future reconnect attempts.
	/// </summary>
	private async Task DismissReconnectDialog() =>
		await MarkSavedPrinterDisconnectedAsync();
}
