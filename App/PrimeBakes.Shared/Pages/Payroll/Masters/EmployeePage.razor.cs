using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Exports.Payroll.Masters;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Payroll.Masters;

public partial class EmployeePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private EmployeeModel _employee = new();
	private LocationModel _selectedLocation;
	private DepartmentModel _selectedDepartment;
	private DesignationModel _selectedDesignation;
	private UserModel _selectedUser;

	private List<EmployeeModel> _employees = [];
	private List<LocationModel> _locations = [];
	private List<DepartmentModel> _departments = [];
	private List<DesignationModel> _designations = [];
	private List<UserModel> _users = [];
	private readonly List<string> _genders = ["MALE", "FEMALE"];
	private readonly List<string> _paymentModes = ["BANK", "CASH", "CHEQUE"];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<EmployeeModel> _sfGrid;
	private CustomTextField _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Payroll], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_employees = await CommonData.LoadTableData<EmployeeModel>(PayrollNames.Employee);
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_departments = await CommonData.LoadTableDataByStatus<DepartmentModel>(PayrollNames.Department);
		_designations = await CommonData.LoadTableDataByStatus<DesignationModel>(PayrollNames.Designation);
		_users = await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User);

		_locations = [.. _locations.OrderBy(l => l.Name)];
		_departments = [.. _departments.OrderBy(d => d.Name)];
		_designations = [.. _designations.OrderBy(d => d.Name)];
		_users = [.. _users.OrderBy(u => u.Name)];

		ResolveSelections();

		if (!_showDeleted)
			_employees = [.. _employees.Where(e => e.Status)];

		if (_sfGrid is not null) await _sfGrid.Refresh();
		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private void ResolveSelections()
	{
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _employee.LocationId);
		_selectedDepartment = _departments.FirstOrDefault(d => d.Id == _employee.DepartmentId);
		_selectedDesignation = _designations.FirstOrDefault(d => d.Id == _employee.DesignationId);
		_selectedUser = _users.FirstOrDefault(u => u.Id == _employee.UserId);
	}
	#endregion

	#region Changed Events
	private DateTime DateOfJoiningDateTime =>
		_employee.DateOfJoining == default ? default : _employee.DateOfJoining.ToDateTime(TimeOnly.MinValue);

	private void OnDateOfJoiningChanged(DateTime value) => _employee.DateOfJoining = DateOnly.FromDateTime(value);

	private DateTime? DateOfLeavingDateTime
	{
		get => _employee.DateOfLeaving?.ToDateTime(TimeOnly.MinValue);
		set => _employee.DateOfLeaving = value is null ? null : DateOnly.FromDateTime(value.Value);
	}

	private DateTime? DateOfBirthDateTime
	{
		get => _employee.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
		set => _employee.DateOfBirth = value is null ? null : DateOnly.FromDateTime(value.Value);
	}
	#endregion

	#region Saving
	private async Task SaveTransaction()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_employee.LocationId = _selectedLocation?.Id ?? 0;
			_employee.DepartmentId = _selectedDepartment?.Id ?? 0;
			_employee.DesignationId = _selectedDesignation?.Id ?? 0;
			_employee.UserId = _selectedUser?.Id;

			var platform = await PlatformInfo.GetPlatformInfo(FormFactor, LocationService);
			await EmployeeData.SaveTransaction(_employee, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Saved", "Transaction has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, selectedRecords[0].Id);
		if (_employee is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		ResolveSelections();
		StateHasChanged();
		await _firstFocus.FocusAsync();
	}

	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var employee = await CommonData.LoadTableDataById<EmployeeModel>(PayrollNames.Employee, id)
				?? throw new Exception("Transaction not found.");

			var platform = await PlatformInfo.GetPlatformInfo(FormFactor, LocationService);
			if (isRecover) await EmployeeData.RecoverTransaction(employee, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);
			else await EmployeeData.DeleteTransaction(employee, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Success", $"Transaction {employee.Code} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while {(isRecover ? "recovering" : "deleting")} transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} transaction {record.Code}",
			() => DeleteRecoverTransaction(record.Id, !record.Status));
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}
	#endregion

	#region Exporting
	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = EmployeeExport.ExportMaster(_employees, _locations, _departments, _designations, _users, await CommonData.LoadCurrentDateTime(), isExcel ? ReportExportType.Excel : ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<EmployeeModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
