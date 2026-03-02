using System.Text;

using PrimeBakes.Shared.Services;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace PrimeBakes.Services;

/// <summary>
/// Windows implementation of Bluetooth Classic printer service.
/// Uses Windows.Devices.Bluetooth and RFCOMM for discovery, pairing, and data transfer.
/// </summary>
public class BluetoothPrinterService : IBluetoothPrinterService
{
	private RfcommDeviceService _rfcommService;
	private StreamSocket _streamSocket;
	private DataWriter _dataWriter;

	private string _connectedPrinterName = string.Empty;
	private string _connectedPrinterAddress = string.Empty;
	private bool _isConnected;

	/// <inheritdoc />
	public bool IsSupported => true;

	/// <inheritdoc />
	public bool IsEnabled => true; // Windows manages Bluetooth at OS level

	/// <inheritdoc />
	public bool IsConnected => _isConnected;

	/// <inheritdoc />
	public string ConnectedPrinterName => _connectedPrinterName;

	/// <inheritdoc />
	public string ConnectedPrinterAddress => _connectedPrinterAddress;

	/// <inheritdoc />
	public Task<bool> RequestPermissionsAsync() =>
		Task.FromResult(true); // Windows does not require runtime Bluetooth permissions

	/// <inheritdoc />
	public async Task<List<BluetoothDeviceInfo>> DiscoverDevicesAsync(CancellationToken cancellationToken = default)
	{
		var devices = new List<BluetoothDeviceInfo>();
		var seenIds = new HashSet<string>();

		try
		{
			// 1. Discover paired Bluetooth Classic devices via RFCOMM Serial Port Profile
			var rfcommSelector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
			var rfcommDevices = await DeviceInformation.FindAllAsync(rfcommSelector);

			foreach (var deviceInfo in rfcommDevices)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				var id = deviceInfo.Id ?? string.Empty;
				if (seenIds.Add(id))
				{
					devices.Add(new BluetoothDeviceInfo
					{
						Name = deviceInfo.Name ?? "Unknown",
						Address = id,
						IsPaired = true
					});
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Windows RFCOMM discovery failed: {ex.Message}");
		}

		try
		{
			// 2. Discover all paired Bluetooth devices (including those without RFCOMM enumerated yet)
			var pairedSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
			var pairedDevices = await DeviceInformation.FindAllAsync(pairedSelector);

			foreach (var deviceInfo in pairedDevices)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				var id = deviceInfo.Id ?? string.Empty;
				if (seenIds.Add(id))
				{
					devices.Add(new BluetoothDeviceInfo
					{
						Name = deviceInfo.Name ?? "Unknown",
						Address = id,
						IsPaired = true
					});
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Windows paired device discovery failed: {ex.Message}");
		}

		try
		{
			// 3. Discover unpaired (new) nearby Bluetooth devices
			var unpairedSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(false);
			var unpairedDevices = await DeviceInformation.FindAllAsync(unpairedSelector);

			foreach (var deviceInfo in unpairedDevices)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				var id = deviceInfo.Id ?? string.Empty;
				if (seenIds.Add(id))
				{
					devices.Add(new BluetoothDeviceInfo
					{
						Name = deviceInfo.Name ?? "Unknown",
						Address = id,
						IsPaired = false
					});
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Windows unpaired device discovery failed: {ex.Message}");
		}

		return devices;
	}

	/// <inheritdoc />
	public async Task<bool> ConnectAsync(string address)
	{
		try
		{
			// Disconnect existing connection first
			await DisconnectAsync();

			// Try direct RFCOMM connection first (for paired RFCOMM device IDs)
			var rfcommService = await RfcommDeviceService.FromIdAsync(address);

			if (rfcommService is null)
			{
				// Address may be a BluetoothDevice ID (paired or unpaired) — resolve it
				var btDevice = await BluetoothDevice.FromIdAsync(address);
				if (btDevice is null)
					return false;

				// If not paired, initiate OS pairing dialog
				if (btDevice.DeviceInformation?.Pairing?.IsPaired != true)
				{
					var pairingResult = await btDevice.DeviceInformation.Pairing.PairAsync();
					if (pairingResult.Status != DevicePairingResultStatus.Paired
						&& pairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
					{
						return false;
					}

					// Re-fetch after pairing to get updated services
					btDevice = await BluetoothDevice.FromIdAsync(address);
					if (btDevice is null)
						return false;
				}

				// Get RFCOMM services from the Bluetooth device
				var rfcommResult = await btDevice.GetRfcommServicesForIdAsync(
					RfcommServiceId.SerialPort,
					BluetoothCacheMode.Uncached);

				if (rfcommResult.Services.Count == 0)
					return false;

				rfcommService = rfcommResult.Services[0];
			}

			_rfcommService = rfcommService;
			_streamSocket = new StreamSocket();

			await _streamSocket.ConnectAsync(
				_rfcommService.ConnectionHostName,
				_rfcommService.ConnectionServiceName);

			_dataWriter = new DataWriter(_streamSocket.OutputStream);
			_connectedPrinterName = rfcommService.Device?.Name ?? "Unknown";
			_connectedPrinterAddress = address;
			_isConnected = true;

			return true;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Bluetooth connect failed: {ex.Message}");
			await DisconnectAsync();
			return false;
		}
	}

	/// <inheritdoc />
	public async Task DisconnectAsync()
	{
		try
		{
			_dataWriter?.DetachStream();
			_dataWriter?.Dispose();
			_dataWriter = null;

			_streamSocket?.Dispose();
			_streamSocket = null;

			_rfcommService?.Dispose();
			_rfcommService = null;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Bluetooth disconnect error: {ex.Message}");
		}
		finally
		{
			_isConnected = false;
			_connectedPrinterName = string.Empty;
			_connectedPrinterAddress = string.Empty;
		}

		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task<bool> SendDataAsync(byte[] data)
	{
		if (!_isConnected || data is null || data.Length == 0)
			return false;

		try
		{
			if (_dataWriter is not null)
			{
				_dataWriter.WriteBytes(data);
				await _dataWriter.StoreAsync();
				await _dataWriter.FlushAsync();
				return true;
			}

			return false;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Bluetooth send data failed: {ex.Message}");
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
		System.Buffer.BlockCopy(initCommand, 0, fullData, 0, initCommand.Length);
		System.Buffer.BlockCopy(textBytes, 0, fullData, initCommand.Length, textBytes.Length);
		System.Buffer.BlockCopy(lineFeed, 0, fullData, initCommand.Length + textBytes.Length, lineFeed.Length);
		System.Buffer.BlockCopy(cutCommand, 0, fullData, initCommand.Length + textBytes.Length + lineFeed.Length, cutCommand.Length);

		return await SendDataAsync(fullData);
	}
}
