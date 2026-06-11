using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Exports;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

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
	private CustomDatePicker _firstFocus;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
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

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
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

			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, id)
				?? throw new Exception("Transaction not found.");

			var label = $"{financialYear.StartDate:dd-MMM-yyyy} to {financialYear.EndDate:dd-MMM-yyyy}";

			if (isRecover) await FinancialYearData.RecoverTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			else await FinancialYearData.DeleteTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Transaction {label} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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
		var label = $"{record.StartDate:dd-MMM-yyyy} to {record.EndDate:dd-MMM-yyyy}";

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} transaction {label}",
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

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialYearModel> args)
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
