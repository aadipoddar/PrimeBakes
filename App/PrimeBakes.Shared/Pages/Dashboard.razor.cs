using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;

using System.Reflection;

namespace PrimeBakes.Shared.Pages;

public partial class Dashboard
{
	#region Device Info
	private string Factor => FormFactor.GetFormFactor();
	private string Platform => FormFactor.GetPlatform();
	private bool IsMobile => Factor == "Phone" || Factor == "Tablet";
	private static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
	#endregion

	#region Updating
	private bool _isUpdating = false;
	private int _updateProgress = 0;
	private int _timeRemaining = 0;
	private string _updateStatus = "Preparing update...";
	private DateTime _updateStartTime;

	private async Task StartUpdateProcess(bool forceUpdate = false)
	{
		_isLoading = false;
		_isUpdating = true;
		_updateProgress = 0;
		_timeRemaining = 0;
		_updateStartTime = DateTime.Now;
		StateHasChanged();

		// Create a progress reporter
		var progress = new Progress<int>(percent =>
		{
			_updateProgress = percent;
			_updateStatus = percent switch
			{
				< 10 => "Preparing update...",
				< 30 => "Downloading update...",
				< 60 => "Installing update...",
				< 90 => "Finalizing installation...",
				_ => "Almost done..."
			};

			// Calculate estimated time remaining
			if (percent > 0)
			{
				var elapsed = (DateTime.Now - _updateStartTime).TotalSeconds;
				var estimatedTotal = elapsed / percent * 100;
				_timeRemaining = Math.Max(0, (int)(estimatedTotal - elapsed));
			}

			InvokeAsync(StateHasChanged);
		});

		await UpdateService.UpdateAppAsync("aadipoddar", CommonSecrets.DatabaseName, CommonSecrets.DatabaseName, progress, forceUpdate);

		_isUpdating = false;
		StateHasChanged();
	}

	private async Task ForceUpdate()
	{
		if (_isLoading || _isUpdating)
			return;

		if (Factor is "Web")
			NavigationManager.NavigateTo(OperationRouteNames.Dashboard, true);

		else
			await StartUpdateProcess(true);
	}
	#endregion

	#region Load Data
	private const int _defaultBackupReminderDays = 7;

	private UserModel _user;
	private bool _isLoading = true;
	private ToastNotification _toastNotification;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			// Check for updates on Android Phone or Windows Desktop
			var shouldCheckUpdate = Platform.Contains("Android") || Factor.Contains("Desktop");

			if (shouldCheckUpdate)
			{
				var hasUpdate = await UpdateService.CheckForUpdatesAsync("aadipoddar", CommonSecrets.DatabaseName, CommonSecrets.DatabaseName, AppVersion);
				if (hasUpdate)
					await StartUpdateProcess();
			}

			await LoadData();
		}
		catch
		{
			await AuthService.Logout();
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task LoadData()
	{
		_user = await AuthService.ValidateUser();

		if (Platform.Contains("Android") || Factor is "Web" or "Wasm")
			_ = NotificationService.RegisterDevicePushNotification(_user.Id.ToString());

		await LoadBackupReminder();
	}

	private async Task LoadBackupReminder()
	{
		if (!_user.Admin || _user.LocationId != 1)
			return;

		try
		{
			var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.BackupReminderDays);
			var reminderDays = int.TryParse(setting?.Value, out var days) ? days : _defaultBackupReminderDays;
			var lastBackup = await SyncData.LoadLastBackupDate();
			var elapsed = lastBackup is null ? int.MaxValue : (int)(DateTime.Now - lastBackup.Value).TotalDays;

			if (elapsed >= reminderDays)
				await _toastNotification.ShowAsync("Backup Due",
					lastBackup is null
						? "The backup server has never been updated. Please run a backup from Settings."
						: $"Last backup was {elapsed} days ago. Please run a backup from Settings.",
					ToastType.Warning);
		}
		catch { }
	}
	#endregion
}
