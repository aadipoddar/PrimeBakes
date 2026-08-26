using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Settings;

using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Shared.Components.Page;

public partial class Footer : IAsyncDisposable
{
	[Parameter] public bool ShowVersion { get; set; } = true;

	private const int _defaultRefreshMinutes = 5;
	private decimal _databaseLoad = -1;

	private PeriodicTimer _refreshTimer;
	private CancellationTokenSource _refreshCts;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || FormFactor.GetFormFactor() is not ("Desktop" or "Web" or "Wasm"))
			return;

		await LoadDatabaseLoad();

		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(setting?.Value, out var minutes) && minutes > 0 ? minutes : _defaultRefreshMinutes;

		_refreshCts = new CancellationTokenSource();
		_refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
		_ = RefreshLoop(_refreshCts.Token);
	}

	private async Task LoadDatabaseLoad()
	{
		try
		{
			_databaseLoad = await CommonData.LoadDatabaseLoad();
			StateHasChanged();
		}
		catch { }
	}

	private async Task RefreshLoop(CancellationToken cancellationToken)
	{
		try
		{
			while (await _refreshTimer.WaitForNextTickAsync(cancellationToken))
				await LoadDatabaseLoad();
		}
		catch (OperationCanceledException) { }
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
