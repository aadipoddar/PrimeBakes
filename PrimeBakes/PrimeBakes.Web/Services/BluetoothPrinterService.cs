using System.Text;

using Blazor.Bluetooth;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

/// <summary>
/// Web implementation of Bluetooth printer service using the Web Bluetooth API (BLE).
/// Uses the Blazor.Bluetooth NuGet package to interact with Bluetooth Low Energy devices
/// from Chrome/Edge browsers. Thermal printers that support BLE expose a GATT write
/// characteristic for ESC/POS data transfer.
/// </summary>
/// <remarks>
/// Web Bluetooth limitations:
/// - Only works in secure contexts (HTTPS or localhost)
/// - Only Chrome, Edge, and Opera support Web Bluetooth (not Firefox/Safari)
/// - Discovery shows a browser-native device picker (user selects one device at a time)
/// - Only supports BLE (Bluetooth Low Energy), not Bluetooth Classic RFCOMM
/// - BLE has a max write payload of ~512 bytes per write, so large data is chunked
/// </remarks>
public class BluetoothPrinterService(IBluetoothNavigator bluetoothNavigator) : IBluetoothPrinterService
{
    /// <summary>
    /// Common GATT service UUIDs used by BLE thermal printers.
    /// Different manufacturers use different UUIDs — we try each until one works.
    /// </summary>
    private static readonly string[] PrinterServiceUuids =
    [
        "000018f0-0000-1000-8000-00805f9b34fb", // Common BLE printer service
        "e7810a71-73ae-499d-8c15-faa9aef0c3f2", // Nordic UART / many Chinese printers
        "0000ff00-0000-1000-8000-00805f9b34fb", // Generic vendor printer service
        "49535343-fe7d-4ae5-8fa9-9fafd205e455", // Microchip / ISSC transparent UART
    ];

    /// <summary>
    /// Common GATT characteristic UUIDs for writing data to BLE thermal printers.
    /// </summary>
    private static readonly string[] PrinterWriteCharacteristicUuids =
    [
        "00002af1-0000-1000-8000-00805f9b34fb", // Common BLE printer write
        "bef8d6c9-9c21-4c9e-b632-bd58c1009f9f", // Nordic UART TX
        "0000ff02-0000-1000-8000-00805f9b34fb", // Generic vendor write
        "49535343-8841-43f4-a8d4-ecbe34729bb3", // ISSC transparent TX
    ];

    /// <summary>
    /// Maximum BLE write payload size per chunk.
    /// Most BLE printers negotiate a small MTU (default ATT MTU = 23 → 20-byte payload).
    /// 100 bytes is a safe upper bound that works with virtually all BLE thermal printers.
    /// </summary>
    private const int MaxBleChunkSize = 100;

    private IDevice? _connectedDevice;
    private IBluetoothRemoteGATTCharacteristic? _writeCharacteristic;
    private bool _isSupported;
    private bool _availabilityChecked;

    private string _connectedPrinterName = string.Empty;
    private string _connectedPrinterAddress = string.Empty;
    private bool _isConnected;

    /// <inheritdoc />
    public bool IsSupported => _availabilityChecked ? _isSupported : true; // Default optimistic until checked

    /// <inheritdoc />
    public bool IsEnabled => IsSupported;

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public string ConnectedPrinterName => _connectedPrinterName;

    /// <inheritdoc />
    public string ConnectedPrinterAddress => _connectedPrinterAddress;

    /// <inheritdoc />
    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
            _isSupported = await bluetoothNavigator.GetAvailability();
            _availabilityChecked = true;
            return _isSupported;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Web Bluetooth availability check failed: {ex.Message}");
            _isSupported = false;
            _availabilityChecked = true;
            return false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Web Bluetooth does not support silent enumeration of nearby devices.
    /// Instead, calling this method opens the browser's native Bluetooth device picker.
    /// The user selects a device from the picker, and that single device is returned.
    /// Previously paired devices can be returned via the experimental GetDevices API.
    /// </remarks>
    public async Task<List<BluetoothDeviceInfo>> DiscoverDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<BluetoothDeviceInfo>();

        try
        {
            // Try to get previously granted devices first (experimental API, may not work in all browsers)
            try
            {
                var knownDevices = await bluetoothNavigator.GetDevices();
                if (knownDevices is not null)
                {
                    foreach (var device in knownDevices)
                    {
                        devices.Add(new BluetoothDeviceInfo
                        {
                            Name = device.Name ?? "Unknown",
                            Address = device.Id ?? string.Empty,
                            IsPaired = true
                        });
                    }
                }
            }
            catch
            {
                // GetDevices requires experimental flags; ignore if unsupported
                System.Diagnostics.Debug.WriteLine("GetDevices not available — experimental flag may be needed.");
            }

            // Open the browser's native device picker for the user to select a new device.
            // We request AcceptAllDevices + all known printer service UUIDs as optional services
            // so we can discover the printer's GATT services after connection.
            var options = new RequestDeviceOptions
            {
                AcceptAllDevices = true,
                OptionalServices = [.. PrinterServiceUuids]
            };

            var selectedDevice = await bluetoothNavigator.RequestDevice(options);

            if (selectedDevice is not null)
            {
                // Avoid duplicates if the device was already in the known-devices list
                var existing = devices.Find(d => d.Address == selectedDevice.Id);
                if (existing is null)
                {
                    devices.Add(new BluetoothDeviceInfo
                    {
                        Name = selectedDevice.Name ?? "Unknown",
                        Address = selectedDevice.Id ?? string.Empty,
                        IsPaired = false
                    });
                }

                // Store the device for later connection
                _connectedDevice = selectedDevice;
            }
        }
        catch (Exception ex) when (ex.Message?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true)
        {
            // User cancelled the browser device picker — not an error
            System.Diagnostics.Debug.WriteLine("User cancelled the Bluetooth device picker.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Web Bluetooth discovery failed: {ex.Message}");
        }

        return devices;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(string address)
    {
        try
        {
            await DisconnectAsync();

            // If we don't have a stored device or the address doesn't match, request a new one
            if (_connectedDevice is null || _connectedDevice.Id != address)
            {
                // Try to find among previously granted devices (no user gesture needed)
                try
                {
                    var knownDevices = await bluetoothNavigator.GetDevices();
                    _connectedDevice = knownDevices?.Find(d => d.Id == address);
                }
                catch
                {
                    // GetDevices not available
                }

                // If the device isn't in memory or previously granted, try requestDevice.
                // This will open the browser's device picker and requires a user gesture (button click).
                // If called without a gesture (e.g. from page load), this will throw and return false.
                if (_connectedDevice is null)
                {
                    try
                    {
                        var options = new RequestDeviceOptions
                        {
                            AcceptAllDevices = true,
                            OptionalServices = [.. PrinterServiceUuids]
                        };
                        _connectedDevice = await bluetoothNavigator.RequestDevice(options);

                        if (_connectedDevice is null)
                            return false;
                    }
                    catch
                    {
                        // requestDevice failed (no user gesture, user cancelled, or unsupported)
                        return false;
                    }
                }
            }

            // Connect to GATT server with retry logic (BLE connections can be flaky)
            const int maxRetries = 3;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _connectedDevice.Gatt.Connect();
                    await Task.Delay(500); // Brief delay for connection stabilization

                    var connected = await _connectedDevice.Gatt.GetConnected();
                    if (connected)
                        break;
                }
                catch when (attempt < maxRetries)
                {
                    System.Diagnostics.Debug.WriteLine($"GATT connect attempt {attempt} failed, retrying...");
                    await Task.Delay(1000);
                }
            }

            if (!_connectedDevice.Gatt.Connected)
            {
                System.Diagnostics.Debug.WriteLine("Failed to connect to GATT server after retries.");
                return false;
            }

            // Discover the printer's write characteristic by trying known service/characteristic UUIDs
            _writeCharacteristic = await DiscoverWriteCharacteristicAsync();

            if (_writeCharacteristic is null)
            {
                System.Diagnostics.Debug.WriteLine("No writable printer GATT characteristic found on device.");
                await _connectedDevice.Gatt.Disonnect();
                return false;
            }

            // Subscribe to disconnect events
            _connectedDevice.OnGattServerDisconnected += OnDeviceDisconnected;

            _connectedPrinterName = _connectedDevice.Name ?? "Unknown";
            _connectedPrinterAddress = _connectedDevice.Id ?? string.Empty;
            _isConnected = true;

            System.Diagnostics.Debug.WriteLine($"Connected to BLE printer: {_connectedPrinterName} [{_connectedPrinterAddress}]");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Web Bluetooth connect failed: {ex.Message}");
            await DisconnectAsync();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        try
        {
            if (_connectedDevice is not null)
            {
                _connectedDevice.OnGattServerDisconnected -= OnDeviceDisconnected;

                if (_connectedDevice.Gatt.Connected)
                    await _connectedDevice.Gatt.Disonnect();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Web Bluetooth disconnect error: {ex.Message}");
        }
        finally
        {
            _writeCharacteristic = null;
            _isConnected = false;
            _connectedPrinterName = string.Empty;
            _connectedPrinterAddress = string.Empty;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendDataAsync(byte[] data)
    {
        if (!_isConnected || _writeCharacteristic is null || data is null || data.Length == 0)
            return false;

        try
        {
            // BLE has a max payload per write; chunk data if needed
            for (var offset = 0; offset < data.Length; offset += MaxBleChunkSize)
            {
                var chunkSize = Math.Min(MaxBleChunkSize, data.Length - offset);
                var chunk = new byte[chunkSize];
                Buffer.BlockCopy(data, offset, chunk, 0, chunkSize);

                // Use WriteWithoutResponse for speed (thermal printers don't need confirmation per write)
                if (_writeCharacteristic.Properties.WriteWithoutResponse)
                    await _writeCharacteristic.WriteValueWithoutResponse(chunk);
                else
                    await _writeCharacteristic.WriteValueWithResponse(chunk);

                // Delay between chunks to avoid overwhelming the BLE buffer
                if (offset + chunkSize < data.Length)
                    await Task.Delay(50);
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Web Bluetooth send data failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> PrintTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // ESC/POS: Initialize printer + text + line feed + cut
        var initCommand = new byte[] { 0x1B, 0x40 }; // ESC @ - Initialize printer
        var textBytes = Encoding.UTF8.GetBytes(text);
        var lineFeed = new byte[] { 0x0A, 0x0A, 0x0A }; // Three line feeds for spacing
        var cutCommand = new byte[] { 0x1D, 0x56, 0x00 }; // GS V 0 - Full cut

        var fullData = new byte[initCommand.Length + textBytes.Length + lineFeed.Length + cutCommand.Length];
        Buffer.BlockCopy(initCommand, 0, fullData, 0, initCommand.Length);
        Buffer.BlockCopy(textBytes, 0, fullData, initCommand.Length, textBytes.Length);
        Buffer.BlockCopy(lineFeed, 0, fullData, initCommand.Length + textBytes.Length, lineFeed.Length);
        Buffer.BlockCopy(cutCommand, 0, fullData, initCommand.Length + textBytes.Length + lineFeed.Length, cutCommand.Length);

        return await SendDataAsync(fullData);
    }

    /// <summary>
    /// Discovers a writable GATT characteristic on the connected printer by trying
    /// known thermal printer service and characteristic UUIDs. Falls back to enumerating
    /// all services and finding any writable characteristic.
    /// </summary>
    /// <returns>The discovered write characteristic, or null if none found.</returns>
    private async Task<IBluetoothRemoteGATTCharacteristic?> DiscoverWriteCharacteristicAsync()
    {
        // Try known printer service + characteristic UUID combinations first
        foreach (var serviceUuid in PrinterServiceUuids)
        {
            try
            {
                var service = await _connectedDevice!.Gatt.GetPrimaryService(serviceUuid);
                if (service is null)
                    continue;

                // Try known write characteristic UUIDs
                foreach (var charUuid in PrinterWriteCharacteristicUuids)
                {
                    try
                    {
                        var characteristic = await service.GetCharacteristic(charUuid);
                        if (characteristic?.Properties is not null &&
                            (characteristic.Properties.Write || characteristic.Properties.WriteWithoutResponse))
                        {
                            System.Diagnostics.Debug.WriteLine($"Found printer write characteristic: service={serviceUuid}, char={charUuid}");
                            return characteristic;
                        }
                    }
                    catch
                    {
                        // Characteristic not found on this service, try next
                    }
                }

                // If no known characteristic matched, try to find any writable characteristic on this service
                try
                {
                    var allCharacteristics = await service.GetCharacteristics();
                    if (allCharacteristics is not null)
                    {
                        foreach (var characteristic in allCharacteristics)
                        {
                            if (characteristic.Properties.Write || characteristic.Properties.WriteWithoutResponse)
                            {
                                System.Diagnostics.Debug.WriteLine($"Found writable characteristic by enumeration: service={serviceUuid}, char={characteristic.Uuid}");
                                return characteristic;
                            }
                        }
                    }
                }
                catch
                {
                    // Service doesn't support GetCharacteristics without UUID, skip
                }
            }
            catch
            {
                // Service not found on this device, try next UUID
            }
        }

        return null;
    }

    /// <summary>
    /// Handles unexpected GATT server disconnection events from the BLE device.
    /// </summary>
    private void OnDeviceDisconnected()
    {
        _isConnected = false;
        _writeCharacteristic = null;
        _connectedPrinterName = string.Empty;
        _connectedPrinterAddress = string.Empty;
        System.Diagnostics.Debug.WriteLine("BLE printer disconnected unexpectedly.");
    }
}
