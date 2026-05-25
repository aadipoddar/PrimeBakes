using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Accounts.Masters.Exports;
using PrimeBakesLibrary.Utils.ExportUtils;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.User.Models;
using PrimeBakesLibrary.Operations.Location.Models;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts.Masters;

public partial class LedgerPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LedgerModel _ledger = new();

	private List<LedgerModel> _ledgers = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<StateUTModel> _stateUTs = [];
	private List<LocationModel> _locations = [];
	private readonly List<ContextMenuItemModel> _ledgerGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditLedger", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverLedger", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<LedgerModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteLedgerId = 0;
	private string _deleteLedgerName = string.Empty;

	private int _recoverLedgerId = 0;
	private string _recoverLedgerName = string.Empty;

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
		_ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		if (!_showDeleted)
			_ledgers = [.. _ledgers.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();
	}
	#endregion

	#region Actions
	private void OnEditLedger(LedgerModel ledger)
	{
		_ledger = new()
		{
			Id = ledger.Id,
			Name = ledger.Name,
			Code = ledger.Code,
			GroupId = ledger.GroupId,
			AccountTypeId = ledger.AccountTypeId,
			StateUTId = ledger.StateUTId,
			GSTNo = ledger.GSTNo,
			PANNo = ledger.PANNo,
			CINNo = ledger.CINNo,
			Alias = ledger.Alias,
			Phone = ledger.Phone,
			Email = ledger.Email,
			Address = ledger.Address,
			Remarks = ledger.Remarks,
			Status = ledger.Status
		};

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

			var ledger = _ledgers.FirstOrDefault(l => l.Id == _deleteLedgerId)
				?? throw new Exception("Ledger not found.");

			await LedgerData.DeleteTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Ledger '{ledger.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.LedgerMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Ledger: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteLedgerId = 0;
			_deleteLedgerName = string.Empty;
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

			var ledger = _ledgers.FirstOrDefault(l => l.Id == _recoverLedgerId)
			 ?? throw new Exception("Ledger not found.");

			await LedgerData.RecoverTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Ledger '{ledger.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.LedgerMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Ledger: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverLedgerId = 0;
			_recoverLedgerName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task SaveLedger()
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

			await LedgerData.SaveTransaction(_ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", $"Ledger '{_ledger.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.LedgerMaster, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Ledger: {ex.Message}", ToastType.Error);
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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.Excel);
			await SaveAndViewService.SaveAndView(fileName, stream);
			await _toastNotification.ShowAsync("Success", "Ledger data exported to Excel successfully.", ToastType.Success);
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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.PDF);
			await SaveAndViewService.SaveAndView(fileName, stream);
			await _toastNotification.ShowAsync("Success", "Ledger data exported to PDF successfully.", ToastType.Success);
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
			case "NewLedger":
				ResetPage();
				break;
			case "SaveLedger":
				await SaveLedger();
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

	private async Task OnLedgerGridContextMenuItemClicked(ContextMenuClickEventArgs<LedgerModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditLedger":
				await EditSelectedItem();
				break;
			case "DeleteRecoverLedger":
				await DeleteSelectedItem();
				break;
		}
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteLedgerId = id;
		_deleteLedgerName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteLedgerId = 0;
		_deleteLedgerName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditLedger(selectedRecords[0]);
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

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverLedgerId = id;
		_recoverLedgerName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverLedgerId = 0;
		_recoverLedgerName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private void ResetPage() =>
		NavigationManager.NavigateTo(PageRouteNames.LedgerMaster, true);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);
	#endregion
}
