using Syncfusion.Blazor.Grids;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Exports;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.User.Models;
using PrimeBakesLibrary.Operations.Settings.Models;
using PrimeBakesLibrary.Utils.ExportUtils;

namespace PrimeBakes.Shared.Pages.Accounts.Masters;

public partial class FinancialYearPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private FinancialYearModel _financialYear = new();

	private List<FinancialYearModel> _financialYears = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<FinancialYearModel> _sfGrid;
	private CustomDatePicker _sfFirstFocus;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts]);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		_financialYears = await CommonData.LoadTableData<FinancialYearModel>(AccountNames.FinancialYear);

		if (!_showDeleted)
			_financialYears = [.. _financialYears.Where(fy => fy.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}
	#endregion

	#region Changed Events
	private DateTime StartDateTime => _financialYear.StartDate == default ? default : _financialYear.StartDate.ToDateTime(TimeOnly.MinValue);
	private DateTime EndDateTime => _financialYear.EndDate == default ? default : _financialYear.EndDate.ToDateTime(TimeOnly.MinValue);

	private void OnStartDateChanged(DateTime value) => _financialYear.StartDate = DateOnly.FromDateTime(value);
	private void OnEndDateChanged(DateTime value) => _financialYear.EndDate = DateOnly.FromDateTime(value);
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

			await FinancialYearData.SaveTransaction(_financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
	private void AutoGenerateNextYear()
	{
		if (_financialYears.Count == 0)
		{
			_financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
			_financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
			_financialYear.YearNo = 1;
		}
		else
		{
			var latestYear = _financialYears
				.Where(fy => fy.Status)
				.OrderByDescending(fy => fy.EndDate)
				.FirstOrDefault();

			if (latestYear != null)
			{
				_financialYear.StartDate = latestYear.EndDate.AddDays(1);
				_financialYear.EndDate = latestYear.EndDate.AddYears(1);
				_financialYear.YearNo = latestYear.YearNo + 1;
			}
			else
			{
				_financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
				_financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
				_financialYear.YearNo = 1;
			}
		}

		_financialYear.Id = 0;
		_financialYear.Remarks = $"Auto-generated based on {(_financialYears.Count == 0 ? "current date" : "latest financial year")}";
		_financialYear.Locked = false;
		_financialYear.Status = true;

		StateHasChanged();
	}

	private async Task DeleteTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, id)
				?? throw new Exception("Transaction not found.");

			await FinancialYearData.DeleteTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Deleted", "Transaction has been deleted successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Deleting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task RecoverTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, id)
				?? throw new Exception("Transaction not found.");

			await FinancialYearData.RecoverTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Recovered", "Transaction has been recovered successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Recovering", ex.Message, ToastType.Error);
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
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.Excel);
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

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.PDF);
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
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": ResetPage(); break;
			case "SaveTransaction": await SaveTransaction(); break;
			case "AutoGenerateNextYear": AutoGenerateNextYear(); break;
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialYearModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, selectedRecords[0].Id);
		if (_financialYear is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];
		var identifier = $"{record.StartDate:dd-MMM-yyyy} to {record.EndDate:dd-MMM-yyyy}";

		if (record.Status)
			await ShowConfirmation("Delete", $"Are you sure you want to delete {identifier}", () => DeleteTransaction(record.Id));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover {identifier}", () => RecoverTransaction(record.Id));
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

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
