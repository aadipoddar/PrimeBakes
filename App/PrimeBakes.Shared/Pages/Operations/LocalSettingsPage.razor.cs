using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Operations.Settings;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Services.Printing;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class LocalSettingsPage
{
	#region Fields

	// UI State
	private bool _isLoading = true;
	private bool _isProcessing;
	private bool _isScanning;
	private bool _isTestPrinting;
	private bool _hasScanned;
	private string _connectingAddress = string.Empty;

	// Toast Reference
	private ToastNotification _toastNotification;

	// Confirmation Dialog
	private ConfirmationDialog _confirmationDialog;
	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	// Bluetooth Devices
	private List<BluetoothDeviceInfo> _discoveredDevices = [];

	#endregion

	#region Lifecycle
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await AuthService.ValidateUser();
			_isLoading = false;
			StateHasChanged();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}
	#endregion

	#region Bluetooth Operations

	/// <summary>
	/// Scans for nearby Bluetooth devices. Requests permissions first on Android.
	/// </summary>
	private async Task ScanForDevices()
	{
		if (_isScanning)
			return;

		try
		{
			_isScanning = true;
			_isProcessing = true;
			_hasScanned = true;
			_discoveredDevices.Clear();
			StateHasChanged();

			// Request permissions (Android requires runtime permissions)
			var permissionsGranted = await BluetoothPrinterService.RequestPermissionsAsync();
			if (!permissionsGranted)
			{
				await _toastNotification.ShowAsync("Permission Denied", "Bluetooth permissions are required to scan for printers. Please grant the permissions in device settings.", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Scanning", "Searching for nearby Bluetooth devices...", ToastType.Info);

			_discoveredDevices = await BluetoothPrinterService.DiscoverDevicesAsync();

			if (_discoveredDevices.Count > 0)
				await _toastNotification.ShowAsync("Scan Complete", $"Found {_discoveredDevices.Count} device(s).", ToastType.Success);
			else
				await _toastNotification.ShowAsync("Scan Complete", "No Bluetooth devices found nearby.", ToastType.Info);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Scan Error", $"Failed to scan: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isScanning = false;
			_isProcessing = false;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Connects to a Bluetooth device by its MAC address.
	/// </summary>
	/// <param name="address">The MAC address of the target Bluetooth device.</param>
	private async Task ConnectToDevice(string address)
	{
		if (_isProcessing || string.IsNullOrWhiteSpace(address))
			return;

		try
		{
			_isProcessing = true;
			_connectingAddress = address;
			StateHasChanged();

			var deviceName = _discoveredDevices.FirstOrDefault(d => d.Address == address)?.DisplayName ?? "Unknown";
			await _toastNotification.ShowAsync("Connecting", $"Connecting to {deviceName}...", ToastType.Info);

			var connected = await BluetoothPrinterService.ConnectAsync(address);

			if (connected)
			{
				VibrationService.VibrateHapticClick();
				await SavePrinterAsync();
				await _toastNotification.ShowAsync("Connected", $"Successfully connected to {BluetoothPrinterService.ConnectedPrinterName}.", ToastType.Success);
			}
			else
			{
				await _toastNotification.ShowAsync("Connection Failed", $"Could not connect to {deviceName}. Make sure the printer is turned on and in range.", ToastType.Error);
			}
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Connection Error", $"Failed to connect: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_connectingAddress = string.Empty;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Disconnects from the currently connected Bluetooth printer.
	/// </summary>
	private async Task DisconnectPrinter()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await BluetoothPrinterService.DisconnectAsync();
			await ClearSavedPrinterAsync();
			await _toastNotification.ShowAsync("Disconnected", "Bluetooth printer disconnected.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to disconnect: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Builds a test receipt and hands it to <see cref="IThermalPrintDispatcher"/>, which routes it to
	/// the Bluetooth printer, the default printer, or the browser print dialog as available.
	/// </summary>
	private async Task TestPrint()
	{
		if (_isTestPrinting)
			return;

		try
		{
			_isTestPrinting = true;
			StateHasChanged();

			var printerName = BluetoothPrinterService.ConnectedPrinterName;
			var printerAddress = BluetoothPrinterService.ConnectedPrinterAddress;
			var platform = $"{FormFactor.GetFormFactor()} / {FormFactor.GetPlatform()}";

			var testPrintCompany = await SettingsData.LoadPrimaryCompany();
			var testPrintDateTime = await CommonData.LoadCurrentDateTime();

			await ThermalPrintDispatcher.PrintAsync(
				() => Task.FromResult(TestPrintExport.GenerateTestReceipt(printerName, printerAddress, platform, testPrintCompany, testPrintDateTime)),
				() => Task.FromResult(TestPrintExport.GenerateTestReceiptPng(printerName, printerAddress, platform, testPrintCompany, testPrintDateTime)));

			VibrationService.VibrateHapticClick();
			await _toastNotification.ShowAsync("Test Print", "Test page sent to printer successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Print Error", $"Test print failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isTestPrinting = false;
			StateHasChanged();
		}
	}

	#endregion

	#region Bluetooth Storage

	/// <summary>
	/// Saves the currently connected printer info to local storage.
	/// </summary>
	private async Task SavePrinterAsync()
	{
		var info = new BluetoothDeviceInfo
		{
			Name = BluetoothPrinterService.ConnectedPrinterName,
			Address = BluetoothPrinterService.ConnectedPrinterAddress,
			IsPaired = true,
			IsConnected = true
		};

		var json = System.Text.Json.JsonSerializer.Serialize(info);
		await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, json);
	}

	private async Task ClearSavedPrinterAsync() =>
		await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);

	#endregion

	#region Local Database

	private async Task ShowInstallConfirmation() =>
		await ShowConfirmation("Install SQL Server",
			"This will install SQL Server Express on this computer if it is not already installed. The download is about 1 GB and takes several minutes. A setup window will open. Continue?",
			InstallSqlServer);

	private async Task InstallSqlServer()
	{
		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Installing", "A setup window has opened. Leave it running until it reports that SQL Server is installed. Restart your computer after the installation is complete.", ToastType.Info);
			await LocalDbService.InstallSqlServer();
			await _toastNotification.ShowAsync("Install Finished", "The setup window has closed. Please restart this computer before continuing.", ToastType.Info);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Install Error", $"Could not start the installer: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SetupDatabase()
	{
		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Setting Up", "Creating and updating the local database. This can take a minute...", ToastType.Info);
			await LocalDbService.SetupDatabase();
			await _toastNotification.ShowAsync("Database Ready", "PrimeBakesClient matches this version of Prime Bakes.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Database Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SyncLocalData()
	{
		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Syncing", "Copying data to this computer...", ToastType.Info);
			var summary = await SyncData.SyncToLocalClient();
			await _toastNotification.ShowAsync("Synced", summary, ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Sync Error", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	#endregion

	#region Uninstall

	private async Task ShowUninstallConfirmation() =>
		await ShowConfirmation("Uninstall",
			"This will log you out and permanently remove Prime Bakes from this computer, including the local database and the SQL Server instance. The app will close immediately. Continue?",
			UninstallApp);

	private async Task UninstallApp()
	{
		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Uninstalling", "Prime Bakes will close and be removed from this computer.", ToastType.Warning);
			await AuthService.Logout();
			await LocalDbService.UninstallSqlServer();
			await UpdateService.UninstallAsync();
		}
		catch (Exception ex)
		{
			_isProcessing = false;
			await _toastNotification.ShowAsync("Uninstall Error", $"Could not start uninstall: {ex.Message}", ToastType.Error);
		}
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}

	#endregion
}
