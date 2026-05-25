using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Accounts.Masters.Exports;
using PrimeBakesLibrary.Utils.ExportUtils;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.User.Models;

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
	private readonly List<ContextMenuItemModel> _financialYearGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditFinancialYear", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverFinancialYear", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<FinancialYearModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteFinancialYearId = 0;
	private string _deleteFinancialYearName = string.Empty;

	private int _recoverFinancialYearId = 0;
	private string _recoverFinancialYearName = string.Empty;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);
		await LoadData();
	}

	private async Task LoadData()
	{
		_financialYears = await CommonData.LoadTableData<FinancialYearModel>(AccountNames.FinancialYear);

		if (!_showDeleted)
			_financialYears = [.. _financialYears.Where(g => g.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();
	}
	#endregion

	#region Actions
	private void OnEditFinancialYear(FinancialYearModel financialYear)
	{
		_financialYear = new()
		{
			Id = financialYear.Id,
			StartDate = financialYear.StartDate,
			EndDate = financialYear.EndDate,
			YearNo = financialYear.YearNo,
			Remarks = financialYear.Remarks,
			Locked = financialYear.Locked,
			Status = financialYear.Status
		};

		StateHasChanged();
	}

	private static string GetFinancialYearName(FinancialYearModel fy) =>
		$"{fy.StartDate:dd-MMM-yyyy} to {fy.EndDate:dd-MMM-yyyy}";

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

		_financialYear.Locked = false;
		_financialYear.Remarks = string.Empty;
		_financialYear.Id = 0;
		_financialYear.Status = true;

		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var financialYear = _financialYears.FirstOrDefault(g => g.Id == _deleteFinancialYearId)
				?? throw new Exception("Financial Year not found.");

			await FinancialYearData.DeleteTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Financial Year '{_deleteFinancialYearName}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.FinancialYearMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Financial Year: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteFinancialYearId = 0;
			_deleteFinancialYearName = string.Empty;
		}
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var financialYear = _financialYears.FirstOrDefault(g => g.Id == _recoverFinancialYearId)
				?? throw new Exception("Financial Year not found.");

			await FinancialYearData.RecoverTransaction(financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Financial Year '{_recoverFinancialYearName}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.FinancialYearMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Financial Year: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverFinancialYearId = 0;
			_recoverFinancialYearName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task SaveFinancialYear()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await FinancialYearData.SaveTransaction(_financialYear, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Financial Year '{_financialYear.StartDate:dd-MMM-yyyy} to {_financialYear.EndDate:dd-MMM-yyyy}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.FinancialYearMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Financial Year: {ex.Message}", ToastType.Error);
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

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);
			await _toastNotification.ShowAsync("Success", "Financial Year data exported to Excel successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to Excel: {ex.Message}", ToastType.Error);
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

			var (stream, fileName) = await FinancialYearExport.ExportMaster(_financialYears, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);
			await _toastNotification.ShowAsync("Success", "Financial Year data exported to PDF successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to PDF: {ex.Message}", ToastType.Error);
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
			case "NewFinancialYear":
				ResetPage();
				break;
			case "SaveFinancialYear":
				await SaveFinancialYear();
				break;
			case "AutoGenerateNextYear":
				AutoGenerateNextYear();
				break;
			case "ToggleDeleted":
				await ToggleDeleted();
				break;
			case "ExportExcel":
				await ExportExcel();
				break;
			case "ExportPdf":
				await ExportPdf();
				break;
			case "EditSelected":
				await EditSelectedItem();
				break;
			case "DeleteRecoverSelected":
				await DeleteSelectedItem();
				break;
		}
	}

	private async Task OnFinancialYearGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialYearModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditFinancialYear":
				await EditSelectedItem();
				break;
			case "DeleteRecoverFinancialYear":
				await DeleteSelectedItem();
				break;
		}
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditFinancialYear(selectedRecords[0]);
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				await ShowDeleteConfirmation(selectedRecords[0].Id, GetFinancialYearName(selectedRecords[0]));
			else
				await ShowRecoverConfirmation(selectedRecords[0].Id, GetFinancialYearName(selectedRecords[0]));
		}
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteFinancialYearId = id;
		_deleteFinancialYearName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteFinancialYearId = 0;
		_deleteFinancialYearName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverFinancialYearId = id;
		_recoverFinancialYearName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverFinancialYearId = 0;
		_recoverFinancialYearName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.FinancialYearMaster, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);
	#endregion
}
