using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class UserPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private UserModel _user = new();
	private LocationModel _location = new();

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteUserId = 0;
	private string _deleteUserName = string.Empty;

	private int _recoverUserId = 0;
	private string _recoverUserName = string.Empty;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Admin], true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.S, SaveUser, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		_users = await CommonData.LoadTableData<UserModel>(TableNames.User);

		if (!_showDeleted)
			_users = [.. _users.Where(u => u.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditUser(UserModel user)
	{
		_user = new()
		{
			Id = user.Id,
			Name = user.Name,
			LocationId = user.LocationId,
			Passcode = user.Passcode,
			Accounts = user.Accounts,
			Inventory = user.Inventory,
			Store = user.Store,
			Restaurant = user.Restaurant,
			Reports = user.Reports,
			Admin = user.Admin,
			Status = user.Status
		};

		_location = _locations.FirstOrDefault(l => l.Id == user.LocationId) ?? new LocationModel();

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteUserId = id;
		_deleteUserName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteUserId = 0;
		_deleteUserName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var user = _users.FirstOrDefault(u => u.Id == _deleteUserId);
			if (user == null)
			{
				await _toastNotification.ShowAsync("Error", "User not found.", ToastType.Error);
				return;
			}

			user.Status = false;
			await UserData.InsertUser(user);

			await _toastNotification.ShowAsync("Deleted", $"User '{user.Name}' removed successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.User, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete user: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteUserId = 0;
			_deleteUserName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverUserId = id;
		_recoverUserName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverUserId = 0;
		_recoverUserName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			var user = _users.FirstOrDefault(u => u.Id == _recoverUserId);
			if (user == null)
			{
				await _toastNotification.ShowAsync("Error", "User not found.", ToastType.Error);
				return;
			}

			user.Status = true;
			await UserData.InsertUser(user);

			await _toastNotification.ShowAsync("Recovered", $"User '{user.Name}' restored successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.User, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover user: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverUserId = 0;
			_recoverUserName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_user.Name = _user.Name?.Trim() ?? "";
		_user.Remarks = _user.Remarks?.Trim() ?? "";

		_user.Name = _user.Name?.ToUpper() ?? "";
		_user.Status = true;

		if (string.IsNullOrWhiteSpace(_user.Name))
		{
			await _toastNotification.ShowAsync("Validation", "User name is required.", ToastType.Warning);
			return false;
		}

		if (_user.Passcode.ToString().Length != 4)
		{
			await _toastNotification.ShowAsync("Validation", "Passcode must be a 4-digit number.", ToastType.Warning);
			return false;
		}

		if (_location is null || _location.Id <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a valid location.", ToastType.Warning);
			return false;
		}
		_user.LocationId = _location.Id;

		if (_user.LocationId <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a location.", ToastType.Warning);
			return false;
		}

		if (!_user.Admin &&
		 	!_user.Inventory &&
			!_user.Accounts &&
			!_user.Store &&
			!_user.Restaurant &&
			!_user.Reports)
		{
			await _toastNotification.ShowAsync("Validation", "At least one role must be assigned.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_user.Remarks))
			_user.Remarks = null;

		if (_user.Id > 0)
		{
			var existingUser = _users.FirstOrDefault(_ => _.Id != _user.Id && _.Passcode == _user.Passcode);
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Passcode '{_user.Passcode}' already exists.", ToastType.Warning);
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Id != _user.Id && _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"User name '{_user.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existingUser = _users.FirstOrDefault(_ => _.Passcode == _user.Passcode);
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Passcode '{_user.Passcode}' already exists.", ToastType.Warning);
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"User name '{_user.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}

		return true;
	}

	private async Task SaveUser()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Saving", "Processing user...", ToastType.Info);

			await UserData.InsertUser(_user);

			await _toastNotification.ShowAsync("Saved", $"User '{_user.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.User, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save user: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Exporting to Excel...", ToastType.Info);

			var (stream, fileName) = await UserExport.ExportMaster(_users, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "User data exported to Excel successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Excel export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Exporting to PDF...", ToastType.Info);

			var (stream, fileName) = await UserExport.ExportMaster(_users, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "User data exported to PDF successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"PDF export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditUser(selectedRecords[0]);
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				await ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
			else
				await ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
		}
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.User, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.OperationsDashboard);

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task Logout() =>
		await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
		GC.SuppressFinalize(this);
	}
	#endregion
}