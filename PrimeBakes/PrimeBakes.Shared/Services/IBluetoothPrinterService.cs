namespace PrimeBakes.Shared.Services;

/// <summary>
/// Service interface for discovering and connecting to Bluetooth thermal printers.
/// Implementations are platform-specific: Android uses Bluetooth Classic APIs,
/// Windows uses Windows.Devices.Bluetooth, and Web provides a no-op fallback.
/// </summary>
public interface IBluetoothPrinterService
{
    /// <summary>
    /// Gets whether Bluetooth is supported on this device/platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets whether Bluetooth is currently enabled on the device.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets whether a Bluetooth printer is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the name of the currently connected printer, or null if not connected.
    /// </summary>
    string ConnectedPrinterName { get; }

    /// <summary>
    /// Gets the MAC address of the currently connected printer, or null if not connected.
    /// </summary>
    string ConnectedPrinterAddress { get; }

    /// <summary>
    /// Discovers nearby Bluetooth devices (both paired and newly discovered).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the discovery process.</param>
    /// <returns>A list of discovered Bluetooth devices.</returns>
    Task<List<BluetoothDeviceInfo>> DiscoverDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the specified Bluetooth printer device.
    /// </summary>
    /// <param name="address">The MAC address of the device.</param>
    /// <returns>True if the connection was successful; otherwise, false.</returns>
    Task<bool> ConnectAsync(string address);

    /// <summary>
    /// Disconnects from the currently connected Bluetooth printer.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends raw byte data to the connected Bluetooth thermal printer.
    /// Used for ESC/POS commands and raw text printing.
    /// </summary>
    /// <param name="data">The raw bytes to send to the printer.</param>
    /// <returns>True if data was sent successfully; otherwise, false.</returns>
    Task<bool> SendDataAsync(byte[] data);

    /// <summary>
    /// Sends a text string to the connected Bluetooth thermal printer using ESC/POS encoding.
    /// </summary>
    /// <param name="text">The text content to print.</param>
    /// <returns>True if the text was sent successfully; otherwise, false.</returns>
    Task<bool> PrintTextAsync(string text);

    /// <summary>
    /// Requests Bluetooth permissions from the user (platform-specific).
    /// </summary>
    /// <returns>True if all required permissions were granted; otherwise, false.</returns>
    Task<bool> RequestPermissionsAsync();
}

/// <summary>
/// Represents a discovered Bluetooth device with its name and hardware address.
/// </summary>
public class BluetoothDeviceInfo
{
    /// <summary>
    /// The display name of the Bluetooth device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The MAC hardware address of the Bluetooth device.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the device is already paired with this device.
    /// </summary>
    public bool IsPaired { get; set; }

    /// <summary>
    /// Indicates whether the last connection attempt to this device was successful.
    /// Set to false when a reconnect attempt fails so the device is remembered for future reconnects.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// The signal strength (RSSI) if available, otherwise null.
    /// </summary>
    public int? SignalStrength { get; set; }

    /// <summary>
    /// A display-friendly representation of the device.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Unknown ({Address})" : Name;
}
