using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Settings;

using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Shared.Components.Page;

public partial class Footer : IAsyncDisposable
{
	[Parameter] public bool ShowVersion { get; set; } = true;

	private const int _defaultRefreshMinutes = 30;
	private decimal _databaseLoad = -1;
	private bool _localDatabaseAvailable;
	private string _platformInfo;

	private PeriodicTimer _refreshTimer;
	private CancellationTokenSource _refreshCts;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_ = LoadPlatformInfo();
		_ = LoadDatabaseLoad();
		_ = LoadLocalDatabase();
		_ = LocalDbService.SyncDataBackground();

		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(setting?.Value, out var minutes) && minutes > 0 ? minutes : _defaultRefreshMinutes;

		_refreshCts = new CancellationTokenSource();
		_refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
		_ = RefreshLoop(_refreshCts.Token);
	}

	private async Task LoadPlatformInfo()
	{
		var platform = await PlatformInfo.GetPlatformInfo();

		_platformInfo = $"Form Factor: {platform.FormFactor}" +
						$" Platform: {platform.Platform}" +
						$" Latitude: {platform.Latitude?.ToString("F6") ?? "N/A"}" +
						$" Longitude: {platform.Longitude?.ToString("F6") ?? "N/A"}";

		await InvokeAsync(StateHasChanged);
	}

	private async Task LoadLocalDatabase()
	{
		if (FormFactor.GetFormFactor() is not "Desktop")
			return;

		_localDatabaseAvailable = await LocalDbService.LocalDBAvailable();
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

	private string DatabaseLoadClass => _databaseLoad switch
	{
		< 50 => "load-low",
		< 80 => "load-medium",
		_ => "load-high"
	};

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
}
