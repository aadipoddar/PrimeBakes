using PrimeBakes.Data.Operations.Notification;

using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class SendNotificationPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private string _notificationTitle = string.Empty;
	private string _notificationText = string.Empty;
	private string _confirmMessage = string.Empty;

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private CustomTextField _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Admin], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableData<LocationModel>(OperationNames.Location, useLocalDB: true);
		_users = [.. (await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User, true, useLocalDB: true)).OrderBy(u => u.Name)];

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}
	#endregion

	#region Saving
	private async Task ConfirmSend()
	{
		if (_isProcessing)
			return;

		if (string.IsNullOrWhiteSpace(_notificationTitle) || string.IsNullOrWhiteSpace(_notificationText))
		{
			await _toastNotification.ShowAsync("Cannot Send", "Please enter a title and a message.", ToastType.Warning);
			return;
		}

		var selected = await _sfGrid.GetSelectedRecordsAsync();

		if (selected is null || selected.Count == 0)
		{
			await _toastNotification.ShowAsync("Cannot Send", "Please select at least one user.", ToastType.Warning);
			return;
		}

		_confirmMessage = $"Send this notification to {selected.Count} user(s)?";
		StateHasChanged();

		await _confirmationDialog.ShowAsync();
	}

	private async Task SendNotification()
	{
		await _confirmationDialog.HideAsync();

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Sending notification...", ToastType.Info);

			var selected = await _sfGrid.GetSelectedRecordsAsync();
			var platform = await PlatformInfo.GetPlatformInfo();

			await NotificationData.SendCustomNotification(
				[.. selected.Select(u => u.Id)],
				_notificationTitle,
				_notificationText,
				_user.Id,
				platform.FormFactor,
				platform.Platform,
				platform.Latitude,
				platform.Longitude);

			await _toastNotification.ShowAsync("Sent", $"Notification sent to {selected.Count} user(s).", ToastType.Success);
			await ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Sending", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
	}

	private async Task OnCancelled() =>
		await _confirmationDialog.HideAsync();
	#endregion

	#region Utilities
	private async Task ResetPage()
	{
		_notificationTitle = string.Empty;
		_notificationText = string.Empty;

		await _sfGrid.ClearSelectionAsync();
		StateHasChanged();

		await _firstFocus.FocusAsync();
	}
	#endregion
}
