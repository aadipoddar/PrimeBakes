using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Exports.Payroll.Masters;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Payroll.Masters;

public partial class EmployeeSalaryComponentPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private EmployeeSalaryComponentModel _employeeSalaryComponent = new();
	private DateTime _effectiveDate = DateTime.Now;
	private EmployeeModel _selectedEmployee;
	private SalaryComponentModel _selectedSalaryComponent;
	private string _formulaPlaceholder = "Leave empty to use the master formula";

	private List<EmployeeSalaryComponentModel> _employeeSalaryComponents = [];
	private List<EmployeeSalaryComponentOverviewModel> _employeeSalaryComponentOverviews = [];
	private List<EmployeeModel> _employees = [];
	private List<SalaryComponentModel> _salaryComponents = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" },
		new() { Text = "Discontinue", Id = "DiscontinueSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<EmployeeSalaryComponentOverviewModel> _sfGrid;
	private CustomAutoComplete<EmployeeModel> _firstFocus;
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
		_effectiveDate = await CommonData.LoadCurrentDateTime();

		_employeeSalaryComponents = await CommonData.LoadTableData<EmployeeSalaryComponentModel>(PayrollNames.EmployeeSalaryComponent);
		_employees = await CommonData.LoadTableDataByStatus<EmployeeModel>(PayrollNames.Employee);
		_salaryComponents = await CommonData.LoadTableDataByStatus<SalaryComponentModel>(PayrollNames.SalaryComponent);

		_employees = [.. _employees.OrderBy(e => e.Name)];
		_salaryComponents = [.. _salaryComponents.OrderBy(c => c.Sequence)];

		await LoadOverviews();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadOverviews()
	{
		if (_employeeSalaryComponent.EmployeeId > 0)
			_employeeSalaryComponentOverviews = await EmployeeSalaryComponentData.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate(EmployeeId: _employeeSalaryComponent.EmployeeId);
		else
			_employeeSalaryComponentOverviews = await CommonData.LoadTableData<EmployeeSalaryComponentOverviewModel>(PayrollNames.EmployeeSalaryComponentOverview);

		_employeeSalaryComponentOverviews = [.. _employeeSalaryComponentOverviews.OrderBy(x => x.EmployeeName).ThenBy(x => x.Sequence)];

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}
	#endregion

	#region Changed Events
	private async Task OnEmployeeChanged()
	{
		_employeeSalaryComponent.EmployeeId = _selectedEmployee?.Id ?? 0;
		await LoadOverviews();
		StateHasChanged();
	}

	private void OnSalaryComponentChanged()
	{
		_employeeSalaryComponent.SalaryComponentId = _selectedSalaryComponent?.Id ?? 0;

		_formulaPlaceholder = _selectedSalaryComponent is null
			? "Leave empty to use the master formula"
			: string.IsNullOrWhiteSpace(_selectedSalaryComponent.Formula)
				? "Master has no formula - the amount above is used"
				: $"Leave empty to use the master formula: {_selectedSalaryComponent.Formula}";

		if (_selectedSalaryComponent is null || _employeeSalaryComponent.EmployeeId <= 0)
			return;

		// New dated entry; default to the values currently in effect for this employee.
		_employeeSalaryComponent.Id = 0;

		var asOn = DateOnly.FromDateTime(_effectiveDate);
		var current = _employeeSalaryComponentOverviews
			.Where(x => x.SalaryComponentId == _employeeSalaryComponent.SalaryComponentId && x.FromDate <= asOn)
			.OrderByDescending(x => x.FromDate)
			.FirstOrDefault();

		_employeeSalaryComponent.Amount = current?.Amount ?? 0;
		_employeeSalaryComponent.Formula = current?.Formula;
		_employeeSalaryComponent.Prorate = current?.Prorate ?? _selectedSalaryComponent.Prorate;
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

			_employeeSalaryComponent.EmployeeId = _selectedEmployee?.Id ?? 0;
			_employeeSalaryComponent.SalaryComponentId = _selectedSalaryComponent?.Id ?? 0;
			_employeeSalaryComponent.FromDate = DateOnly.FromDateTime(_effectiveDate);
			await EmployeeSalaryComponentData.SaveTransaction(_employeeSalaryComponent, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));

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

		var employeeSalaryComponent = _employeeSalaryComponents.FirstOrDefault(x => x.Id == selectedRecords[0].Id);
		if (employeeSalaryComponent is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_employeeSalaryComponent = new()
		{
			Id = employeeSalaryComponent.Id,
			EmployeeId = employeeSalaryComponent.EmployeeId,
			SalaryComponentId = employeeSalaryComponent.SalaryComponentId,
			Amount = employeeSalaryComponent.Amount,
			Formula = employeeSalaryComponent.Formula,
			Prorate = employeeSalaryComponent.Prorate,
			FromDate = employeeSalaryComponent.FromDate,
			Remarks = employeeSalaryComponent.Remarks
		};
		_effectiveDate = employeeSalaryComponent.FromDate.ToDateTime(TimeOnly.MinValue);

		_selectedEmployee = _employees.FirstOrDefault(e => e.Id == employeeSalaryComponent.EmployeeId);
		_selectedSalaryComponent = _salaryComponents.FirstOrDefault(c => c.Id == employeeSalaryComponent.SalaryComponentId);

		StateHasChanged();
		await _firstFocus.FocusAsync();
	}

	private async Task DeleteTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			await EmployeeSalaryComponentData.DeleteTransaction(employeeSalaryComponent, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));

			await _toastNotification.ShowAsync("Success", $"Transaction {employeeSalaryComponent.SalaryComponentName} has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DiscontinueTransaction(EmployeeSalaryComponentOverviewModel employeeSalaryComponent)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", "Discontinuing transaction...", ToastType.Info);

			await EmployeeSalaryComponentData.DiscontinueTransaction(employeeSalaryComponent, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));

			await _toastNotification.ShowAsync("Success", $"Transaction {employeeSalaryComponent.SalaryComponentName} has been discontinued successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while discontinuing transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation("Delete Component",
			$"Are you sure you want to delete {record.SalaryComponentName} for {record.EmployeeName} effective {record.FromDate:dd-MMM-yyyy}?",
			() => DeleteTransaction(record));
	}

	private async Task DiscontinueSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation("Discontinue Component",
			$"Are you sure you want to discontinue {record.SalaryComponentName} for {record.EmployeeName}? Every dated entry will be removed.",
			() => DiscontinueTransaction(record));
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

			var (stream, fileName) = EmployeeSalaryComponentExport.ExportMaster(_employeeSalaryComponentOverviews, await CommonData.LoadCurrentDateTime(), isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<EmployeeSalaryComponentOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteSelectedItem": await DeleteSelectedItem(); break;
			case "DiscontinueSelectedItem": await DiscontinueSelectedItem(); break;
		}
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
