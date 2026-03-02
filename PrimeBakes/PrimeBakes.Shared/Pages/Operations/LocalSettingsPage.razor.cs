using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes.Shared.Pages.Operations;

/// <summary>
/// Code-behind for the Local Device Settings page.
/// Handles Bluetooth thermal printer discovery, connection, and management.
/// </summary>
public partial class LocalSettingsPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;

    #region Fields

    // UI State
    private bool _isLoading = true;
    private bool _isProcessing;
    private bool _isScanning;
    private bool _hasScanned;
    private string _connectingAddress = string.Empty;

    // Toast Reference
    private ToastNotification _toastNotification;

    // Bluetooth Devices
    private List<BluetoothDeviceInfo> _discoveredDevices = [];
    private CancellationTokenSource _scanCancellationTokenSource;

    #endregion

    #region Lifecycle

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None);

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

            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = new CancellationTokenSource();

            _discoveredDevices = await BluetoothPrinterService.DiscoverDevicesAsync(_scanCancellationTokenSource.Token);

            if (_discoveredDevices.Count > 0)
                await _toastNotification.ShowAsync("Scan Complete", $"Found {_discoveredDevices.Count} device(s).", ToastType.Success);
            else
                await _toastNotification.ShowAsync("Scan Complete", "No Bluetooth devices found nearby.", ToastType.Info);
        }
        catch (OperationCanceledException)
        {
            await _toastNotification.ShowAsync("Scan Cancelled", "Bluetooth scan was cancelled.", ToastType.Info);
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
    /// Cancels an ongoing Bluetooth device scan.
    /// </summary>
    private void CancelScan()
    {
        _scanCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Connects to a Bluetooth device by its MAC address.
    /// </summary>
    /// <param name="address">The MAC address of the target Bluetooth device.</param>
    private async Task ConnectToDevice(string address)
    {
        if (_isProcessing || string.IsNullOrEmpty(address))
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
            IsPaired = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(info);
        await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, json);
    }

    private async Task ClearSavedPrinterAsync() =>
        await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);

    #endregion

    #region Utilities

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.OperationsDashboard);

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

    public async ValueTask DisposeAsync()
    {
        _scanCancellationTokenSource?.Cancel();
        _scanCancellationTokenSource?.Dispose();

        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    #endregion
}
