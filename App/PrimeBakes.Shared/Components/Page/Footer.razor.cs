using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Operations.Terminal;

using PrimeBakes.Models.Operations.Maintenance;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Shared.Components.Page;

public partial class Footer : IAsyncDisposable
{
	#region Load Data
	[Parameter] public bool ShowVersion { get; set; } = true;

	private decimal _databaseLoad = -1;
	private bool _localDatabaseAvailable;
	private DateTime? _lastSyncedAt;
	private string _platformInfo;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_ = LoadPlatformInfo();
		_ = LoadDatabaseLoad();
		_ = LoadLocalDatabase();
		_ = LocalDbService.SyncDataBackground();

		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		_refreshMinutes = int.TryParse(setting?.Value, out var minutes) && minutes > 0 ? minutes : _defaultRefreshMinutes;

		_refreshCts = new CancellationTokenSource();
		_refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(_refreshMinutes));
		_ = RefreshLoop(_refreshCts.Token);
	}

	private async Task LoadPlatformInfo()
	{
		var platform = await PlatformInfo.GetPlatformInfo();
		var terminal = await LoadTerminal();

		_platformInfo = $"Form: {platform.FormFactor}" +
						$" Platform: {platform.Platform}" +
						$" Lat: {platform.Latitude?.ToString("F6") ?? "N/A"}" +
						$" Long: {platform.Longitude?.ToString("F6") ?? "N/A"}" +
						(terminal is null ? string.Empty : $" Terminal: T{terminal.TerminalNo}");

		await InvokeAsync(StateHasChanged);
	}

	private async Task<TerminalModel> LoadTerminal()
	{
		var machineId = FormFactor.GetMachineId();

		if (string.IsNullOrWhiteSpace(machineId))
			return null;

		try
		{
			return await TerminalData.LoadTerminalByMachineId(machineId);
		}
		catch
		{
			return null;
		}
	}

	private async Task LoadLocalDatabase()
	{
		if (FormFactor.GetFormFactor() is not "Desktop")
			return;

		_localDatabaseAvailable = await LocalDbService.LocalDBAvailable();

		_lastSyncedAt = _localDatabaseAvailable
			? (await CommonData.LoadTableData<SyncVersionModel>(OperationNames.SyncVersion, useLocalDB: true)).Max(sync => (DateTime?)sync.LastSyncedAt)
			: null;

		await InvokeAsync(StateHasChanged);
	}

	private async Task LoadDatabaseLoad()
	{
		if (FormFactor.GetFormFactor() is not ("Desktop" or "Web" or "Wasm"))
			return;

		try
		{
			_databaseLoad = await CommonData.LoadDatabaseLoad();
			await InvokeAsync(StateHasChanged);
		}
		catch { }
	}
	#endregion

	#region Refresh
	private const int _defaultRefreshMinutes = 30;
	private int _refreshMinutes = _defaultRefreshMinutes;

	private PeriodicTimer _refreshTimer;
	private CancellationTokenSource _refreshCts;

	private async Task RefreshLoop(CancellationToken cancellationToken)
	{
		try
		{
			while (await _refreshTimer.WaitForNextTickAsync(cancellationToken))
			{
				await LoadPlatformInfo();
				await LoadDatabaseLoad();
				await LoadLocalDatabase();
				await AuthService.ValidateUser();
				_ = LocalDbService.SyncDataBackground();
			}
		}
		catch { }
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_refreshCts is not null)
		{
			await _refreshCts.CancelAsync();
			_refreshCts.Dispose();
		}

		_refreshTimer?.Dispose();
		GC.SuppressFinalize(this);
	}
	#endregion

	#region Utilities
	private string LastSyncedText => $"Synced {FormatAge(DateTime.Now - _lastSyncedAt.Value)}";

	private string LastSyncedClass =>
		DateTime.Now - _lastSyncedAt.Value <= TimeSpan.FromMinutes(_refreshMinutes) ? "load-low" : "load-high";

	private static string FormatAge(TimeSpan age) => age switch
	{
		{ TotalMinutes: < 1 } => "Just Now",
		{ TotalHours: < 1 } => $"{age.TotalMinutes:N0}m Ago",
		{ TotalDays: < 1 } => $"{age.TotalHours:N0}h Ago",
		_ => $"{age.TotalDays:N0}d Ago"
	};

	private string DatabaseLoadClass => _databaseLoad < 70 ? "load-low" : "load-high";
	#endregion
}
