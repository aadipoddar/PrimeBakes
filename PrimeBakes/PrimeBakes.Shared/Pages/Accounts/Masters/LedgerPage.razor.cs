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

public partial class LedgerPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LedgerModel _ledger = new();
	private GroupModel _selectedGroup;
	private AccountTypeModel _selectedAccountType;
	private StateUTModel _selectedStateUT;

	private List<LedgerModel> _ledgers = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<StateUTModel> _stateUTs = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<LedgerModel> _sfGrid;
	private CustomTextField _sfFirstFocus;
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
		_ledgers = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(AccountNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(AccountNames.StateUT);

		_groups = [.. _groups.OrderBy(g => g.Name)];
		_accountTypes = [.. _accountTypes.OrderBy(a => a.Name)];
		_stateUTs = [.. _stateUTs.OrderBy(s => s.Name)];

		_selectedGroup = _groups.FirstOrDefault(g => g.Id == _ledger.GroupId);
		_selectedAccountType = _accountTypes.FirstOrDefault(a => a.Id == _ledger.AccountTypeId);
		_selectedStateUT = _stateUTs.FirstOrDefault(s => s.Id == _ledger.StateUTId);

		if (!_showDeleted)
			_ledgers = [.. _ledgers.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
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

			_ledger.GroupId = _selectedGroup?.Id ?? 0;
			_ledger.AccountTypeId = _selectedAccountType?.Id ?? 0;
			_ledger.StateUTId = _selectedStateUT?.Id ?? 0;
			await LedgerData.SaveTransaction(_ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
	private async Task DeleteTransaction(int id)
	{
		try
		{
			_isProcessing = true;

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			var ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, id)
				?? throw new Exception("Transaction not found.");

			await LedgerData.DeleteTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, id)
				?? throw new Exception("Transaction not found.");

			await LedgerData.RecoverTransaction(ledger, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.Excel);
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

			var (stream, fileName) = await LedgerExport.ExportMaster(_ledgers, ReportExportType.PDF);
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
			case "ToggleDeleted": await ToggleDeleted(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<LedgerModel> args)
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

		_ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, selectedRecords[0].Id);
		if (_ledger is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedGroup = _groups.FirstOrDefault(g => g.Id == _ledger.GroupId);
		_selectedAccountType = _accountTypes.FirstOrDefault(a => a.Id == _ledger.AccountTypeId);
		_selectedStateUT = _stateUTs.FirstOrDefault(s => s.Id == _ledger.StateUTId);
		StateHasChanged();
		await _sfFirstFocus.FocusAsync();
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		if (record.Status)
			await ShowConfirmation("Delete", $"Are you sure you want to delete {record.Name}", () => DeleteTransaction(record.Id));
		else
			await ShowConfirmation("Recover", $"Are you sure you want to recover {record.Name}", () => RecoverTransaction(record.Id));
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
