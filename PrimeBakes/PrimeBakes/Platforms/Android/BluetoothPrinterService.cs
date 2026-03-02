using Android.Bluetooth;
using Android.Content;
using Android.OS;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

/// <summary>
/// Android implementation of Bluetooth Classic printer service.
/// Uses BluetoothManager/BluetoothAdapter for discovery and RFCOMM SPP for data transfer.
/// </summary>
public class BluetoothPrinterService : IBluetoothPrinterService
{
	/// <summary>Standard SPP (Serial Port Profile) UUID used by Bluetooth printers.</summary>
	private static readonly Guid SppUuid = Guid.Parse("00001101-0000-1000-8000-00805F9B34FB");

	private BluetoothAdapter _bluetoothAdapter;
	private BluetoothSocket _bluetoothSocket;
	private Stream _outputStream;
	private BluetoothDeviceDiscoveryReceiver _discoveryReceiver;

	private string _connectedPrinterName = string.Empty;
	private string _connectedPrinterAddress = string.Empty;
	private bool _isConnected;

	/// <inheritdoc />
	public bool IsSupported => GetBluetoothAdapter() is not null;

	/// <inheritdoc />
	public bool IsEnabled => GetBluetoothAdapter()?.IsEnabled == true;

	/// <inheritdoc />
	public bool IsConnected => _isConnected;

	/// <inheritdoc />
	public string ConnectedPrinterName => _connectedPrinterName;

	/// <inheritdoc />
	public string ConnectedPrinterAddress => _connectedPrinterAddress;

	/// <inheritdoc />
	public async Task<bool> RequestPermissionsAsync()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
		if (status != PermissionStatus.Granted)
			status = await Permissions.RequestAsync<Permissions.Bluetooth>();

		if (status != PermissionStatus.Granted)
		{
			System.Diagnostics.Debug.WriteLine("Bluetooth permission denied by user.");
			return false;
		}

		// Android 12+ (API 31+): With neverForLocation in the manifest, no location permission needed.
		// The BLUETOOTH_SCAN + BLUETOOTH_CONNECT permissions (handled by Permissions.Bluetooth) are sufficient.
		if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
			return true;

		// Android 11 and below: Location permission is required for Bluetooth Classic discovery.
		var locStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
		if (locStatus != PermissionStatus.Granted)
			locStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

		if (locStatus != PermissionStatus.Granted)
		{
			System.Diagnostics.Debug.WriteLine("Location permission denied — required for BT discovery on Android < 12.");
			return false;
		}

		// On Android < 12, Location Services must also be enabled at the OS level for StartDiscovery() to work.
		var locationManager = Platform.CurrentActivity?.GetSystemService(Context.LocationService) as global::Android.Locations.LocationManager;
		if (locationManager is not null)
		{
			var gpsEnabled = locationManager.IsProviderEnabled(global::Android.Locations.LocationManager.GpsProvider);
			var networkEnabled = locationManager.IsProviderEnabled(global::Android.Locations.LocationManager.NetworkProvider);
			if (!gpsEnabled && !networkEnabled)
			{
				System.Diagnostics.Debug.WriteLine("Location services are disabled — BT discovery will not find new devices.");
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<List<BluetoothDeviceInfo>> DiscoverDevicesAsync(CancellationToken cancellationToken = default)
	{
		var devices = new List<BluetoothDeviceInfo>();

		_bluetoothAdapter = GetBluetoothAdapter();

		if (_bluetoothAdapter is null || !_bluetoothAdapter.IsEnabled)
			return devices;

		// First, add bonded (paired) devices
		var bondedDevices = _bluetoothAdapter.BondedDevices;
		if (bondedDevices is not null)
		{
			foreach (var device in bondedDevices)
			{
				devices.Add(new BluetoothDeviceInfo
				{
					Name = device.Name ?? "Unknown",
					Address = device.Address ?? string.Empty,
					IsPaired = true
				});
			}
		}

		System.Diagnostics.Debug.WriteLine($"Found {devices.Count} bonded device(s). Starting discovery for nearby devices...");

		// Then start discovery for new (unpaired) nearby devices
		var tcs = new TaskCompletionSource<bool>();
		_discoveryReceiver = new BluetoothDeviceDiscoveryReceiver(devices, tcs);

		var activity = Platform.CurrentActivity;
		if (activity is not null)
		{
			var filter = new IntentFilter();
			filter.AddAction(BluetoothDevice.ActionFound);
			filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);

			if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
				activity.RegisterReceiver(_discoveryReceiver, filter, global::Android.Content.ReceiverFlags.Exported);
			else
				activity.RegisterReceiver(_discoveryReceiver, filter);

			// Cancel any in-progress discovery before starting a fresh one
			_bluetoothAdapter.CancelDiscovery();

			var discoveryStarted = _bluetoothAdapter.StartDiscovery();
			System.Diagnostics.Debug.WriteLine($"BluetoothAdapter.StartDiscovery() returned: {discoveryStarted}");

			if (!discoveryStarted)
				System.Diagnostics.Debug.WriteLine("Discovery failed to start — check permissions and Bluetooth state.");
		}

		// Wait up to 12 seconds for discovery, or until cancelled
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

		try
		{
			await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, linkedCts.Token));
		}
		catch (System.OperationCanceledException)
		{
			// Normal cancellation
		}
		finally
		{
			_bluetoothAdapter?.CancelDiscovery();
			try
			{
				activity?.UnregisterReceiver(_discoveryReceiver);
			}
			catch
			{
				// Receiver might not be registered
			}
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

			_bluetoothAdapter = GetBluetoothAdapter();
			if (_bluetoothAdapter is null)
				return false;

			// Cancel discovery to speed up connection
			_bluetoothAdapter.CancelDiscovery();

			var device = _bluetoothAdapter.GetRemoteDevice(address);
			if (device is null)
				return false;

			// If device is not paired, initiate bonding and wait for it to complete
			if (device.BondState != Bond.Bonded)
			{
				var bondTcs = new TaskCompletionSource<bool>();
				var bondReceiver = new BluetoothBondReceiver(address, bondTcs);

				var activity = Platform.CurrentActivity;
				if (activity is null)
					return false;

				var filter = new IntentFilter(BluetoothDevice.ActionBondStateChanged);

				if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
					activity.RegisterReceiver(bondReceiver, filter, global::Android.Content.ReceiverFlags.Exported);
				else
					activity.RegisterReceiver(bondReceiver, filter);

				try
				{
					var bondStarted = device.CreateBond();
					if (!bondStarted)
						return false;

					// Wait up to 30 seconds for the user to complete the pairing dialog
					using var bondTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
					var completedTask = await Task.WhenAny(bondTcs.Task, Task.Delay(Timeout.Infinite, bondTimeout.Token));

					if (completedTask != bondTcs.Task || !bondTcs.Task.Result)
						return false;
				}
				catch (System.OperationCanceledException)
				{
					return false;
				}
				finally
				{
					try { activity.UnregisterReceiver(bondReceiver); } catch { }
				}

				// Re-fetch device after bonding to get updated state
				device = _bluetoothAdapter.GetRemoteDevice(address);
				if (device is null || device.BondState != Bond.Bonded)
					return false;
			}

			_bluetoothSocket = device.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString(SppUuid.ToString()));
			await Task.Run(() => _bluetoothSocket.Connect());

			_outputStream = _bluetoothSocket.OutputStream;
			_connectedPrinterName = device.Name ?? "Unknown";
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
			_outputStream?.Close();
			_outputStream?.Dispose();
			_outputStream = null;

			_bluetoothSocket?.Close();
			_bluetoothSocket?.Dispose();
			_bluetoothSocket = null;
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
			if (_outputStream is not null)
			{
				await _outputStream.WriteAsync(data);
				await _outputStream.FlushAsync();
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
		var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
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
	/// Gets the BluetoothAdapter using the modern BluetoothManager API (preferred over the deprecated static DefaultAdapter).
	/// </summary>
	private static BluetoothAdapter GetBluetoothAdapter()
	{
		var context = Platform.CurrentActivity ?? Platform.AppContext;
		var bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
		return bluetoothManager?.Adapter;
	}
}

/// <summary>
/// BroadcastReceiver that listens for Bluetooth device discovery events on Android.
/// Adds discovered devices to the provided list and signals completion when discovery ends.
/// </summary>
public class BluetoothDeviceDiscoveryReceiver : BroadcastReceiver
{
	private readonly List<BluetoothDeviceInfo> _devices;
	private readonly TaskCompletionSource<bool> _tcs;
	private readonly HashSet<string> _seenAddresses = [];

	public BluetoothDeviceDiscoveryReceiver(List<BluetoothDeviceInfo> devices, TaskCompletionSource<bool> tcs)
	{
		_devices = devices;
		_tcs = tcs;

		// Pre-populate seen addresses from paired devices
		foreach (var d in devices)
		{
			if (!string.IsNullOrEmpty(d.Address))
				_seenAddresses.Add(d.Address);
		}
	}

	public override void OnReceive(Context context, Intent intent)
	{
		var action = intent?.Action;

		if (BluetoothDevice.ActionFound.Equals(action))
		{
			// Use the type-safe API on Android 13+ to avoid deprecation
			BluetoothDevice device = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
				? intent.GetParcelableExtra(BluetoothDevice.ExtraDevice, Java.Lang.Class.FromType(typeof(BluetoothDevice))) as BluetoothDevice
				: intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;

			if (device is not null && !string.IsNullOrEmpty(device.Address) && _seenAddresses.Add(device.Address))
			{
				var rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, short.MinValue);

				_devices.Add(new BluetoothDeviceInfo
				{
					Name = device.Name ?? "Unknown",
					Address = device.Address,
					IsPaired = device.BondState == Bond.Bonded,
					SignalStrength = rssi == short.MinValue ? null : rssi
				});

				System.Diagnostics.Debug.WriteLine($"Discovered device: {device.Name ?? "Unknown"} [{device.Address}] Paired={device.BondState == Bond.Bonded}");
			}
		}
		else if (BluetoothAdapter.ActionDiscoveryFinished.Equals(action))
		{
			System.Diagnostics.Debug.WriteLine($"Discovery finished. Total devices: {_devices.Count}");
			_tcs.TrySetResult(true);
		}
	}
}

/// <summary>
/// BroadcastReceiver that listens for Bluetooth bond state changes on Android.
/// Used to wait for the OS pairing dialog result before opening an RFCOMM socket.
/// </summary>
public class BluetoothBondReceiver : BroadcastReceiver
{
	private readonly string _targetAddress;
	private readonly TaskCompletionSource<bool> _tcs;

	public BluetoothBondReceiver(string targetAddress, TaskCompletionSource<bool> tcs)
	{
		_targetAddress = targetAddress;
		_tcs = tcs;
	}

	public override void OnReceive(Context context, Intent intent)
	{
		if (intent?.Action != BluetoothDevice.ActionBondStateChanged)
			return;

		// Use the type-safe API on Android 13+ to avoid deprecation
		BluetoothDevice device = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
			? intent.GetParcelableExtra(BluetoothDevice.ExtraDevice, Java.Lang.Class.FromType(typeof(BluetoothDevice))) as BluetoothDevice
			: intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;

		if (device is null || device.Address != _targetAddress)
			return;

		var bondState = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraBondState, (int)Bond.None);

		if (bondState == Bond.Bonded)
			_tcs.TrySetResult(true);
		else if (bondState == Bond.None)
			_tcs.TrySetResult(false); // Pairing was rejected or failed
	}
}
