using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts.Masters;

public partial class LedgerPage : IAsyncDisposable
{
    private UserModel _user;
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private LedgerModel _ledger = new();

    private List<LedgerModel> _ledgers = [];
    private List<GroupModel> _groups = [];
    private List<AccountTypeModel> _accountTypes = [];
    private List<StateUTModel> _stateUTs = [];
    private List<LocationModel> _locations = [];

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
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveLedger, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
        _groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
        _accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
        _stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

        if (!_showDeleted)
            _ledgers = [.. _ledgers.Where(l => l.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
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

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            if (!_user.Admin)
            {
                await _toastNotification.ShowAsync("Unauthorized", "You do not have permission to perform this action.", ToastType.Error);
                return;
            }

            var ledger = _ledgers.FirstOrDefault(l => l.Id == _deleteLedgerId);
            if (ledger == null)
            {
                await _toastNotification.ShowAsync("Error", "Ledger not found.", ToastType.Error);
                return;
            }

            ledger.Status = false;
            await LedgerData.InsertLedger(ledger);

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

    private async Task ConfirmRecover()
    {
        try
        {
            _isProcessing = true;
            await _recoverConfirmationDialog.HideAsync();

            if (!_user.Admin)
            {
                await _toastNotification.ShowAsync("Unauthorized", "You do not have permission to perform this action.", ToastType.Error);
                return;
            }

            var ledger = _ledgers.FirstOrDefault(l => l.Id == _recoverLedgerId);
            if (ledger == null)
            {
                await _toastNotification.ShowAsync("Error", "Ledger not found.", ToastType.Error);
                return;
            }

            ledger.Status = true;
            await LedgerData.InsertLedger(ledger);

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
    private async Task<bool> ValidateForm()
    {
        if (!_user.Admin)
        {
            await _toastNotification.ShowAsync("Unauthorized", "You do not have permission to perform this action.", ToastType.Error);
            return false;
        }
        
        _ledger.Name = _ledger.Name?.Trim() ?? "";
        _ledger.Name = _ledger.Name?.ToUpper() ?? "";

        _ledger.GSTNo = _ledger.GSTNo?.Trim() ?? "";
        _ledger.GSTNo = _ledger.GSTNo?.ToUpper() ?? "";

        _ledger.PANNo = _ledger.PANNo?.Trim() ?? "";
        _ledger.PANNo = _ledger.PANNo?.ToUpper() ?? "";

        _ledger.CINNo = _ledger.CINNo?.Trim() ?? "";
        _ledger.CINNo = _ledger.CINNo?.ToUpper() ?? "";

        _ledger.Alias = _ledger.Alias?.Trim() ?? "";
        _ledger.Alias = _ledger.Alias?.ToUpper() ?? "";

        _ledger.Phone = _ledger.Phone?.Trim() ?? "";
        _ledger.Email = _ledger.Email?.Trim() ?? "";
        _ledger.Address = _ledger.Address?.Trim() ?? "";

        _ledger.Remarks = _ledger.Remarks?.Trim() ?? "";
        _ledger.Status = true;

        if (string.IsNullOrWhiteSpace(_ledger.Name))
        {
            await _toastNotification.ShowAsync("Error", "Ledger name is required. Please enter a valid ledger name.", ToastType.Error);
            return false;
        }

        if (_ledger.GroupId <= 0)
        {
            await _toastNotification.ShowAsync("Error", "Group is required. Please select a valid group.", ToastType.Error);
            return false;
        }

        if (_ledger.AccountTypeId <= 0)
        {
            await _toastNotification.ShowAsync("Error", "Account Type is required. Please select a valid account type.", ToastType.Error);
            return false;
        }

        if (_ledger.StateUTId <= 0)
        {
            await _toastNotification.ShowAsync("Error", "State/UT is required. Please select a valid State/UT.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_ledger.GSTNo)) _ledger.GSTNo = null;
        if (string.IsNullOrWhiteSpace(_ledger.PANNo)) _ledger.PANNo = null;
        if (string.IsNullOrWhiteSpace(_ledger.CINNo)) _ledger.CINNo = null;
        if (string.IsNullOrWhiteSpace(_ledger.Alias)) _ledger.Alias = null;
        if (string.IsNullOrWhiteSpace(_ledger.Phone)) _ledger.Phone = null;
        if (string.IsNullOrWhiteSpace(_ledger.Email)) _ledger.Email = null;
        if (string.IsNullOrWhiteSpace(_ledger.Address)) _ledger.Address = null;
        if (string.IsNullOrWhiteSpace(_ledger.Remarks)) _ledger.Remarks = null;

        if (!string.IsNullOrWhiteSpace(_ledger.Phone) && !Helper.ValidatePhoneNumber(_ledger.Phone))
        {
            await _toastNotification.ShowAsync("Error", "Invalid phone number format. Please enter a valid phone number.", ToastType.Error);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_ledger.Email) && !Helper.ValidateEmail(_ledger.Email))
        {
            await _toastNotification.ShowAsync("Error", "Invalid email format. Please enter a valid email address.", ToastType.Error);
            return false;
        }

        if (_ledger.Id > 0)
        {
            var existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                await _toastNotification.ShowAsync("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_ledger.Phone))
            {
                var duplicatePhoneLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.Phone.Equals(_ledger.Phone, StringComparison.OrdinalIgnoreCase));
                if (duplicatePhoneLedger is not null)
                {
                    await _toastNotification.ShowAsync("Error", $"Phone number '{_ledger.Phone}' is already associated with another ledger. Please use a different phone number.", ToastType.Error);
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_ledger.Email))
            {
                var duplicateEmailLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.Email.Equals(_ledger.Email, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmailLedger is not null)
                {
                    await _toastNotification.ShowAsync("Error", $"Email '{_ledger.Email}' is already associated with another ledger. Please use a different email address.", ToastType.Error);
                    return false;
                }
            }
        }
        else
        {
            var existingLedger = _ledgers.FirstOrDefault(_ => _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                await _toastNotification.ShowAsync("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_ledger.Phone))
            {
                var duplicatePhoneLedger = _ledgers.FirstOrDefault(_ => _.Phone.Equals(_ledger.Phone, StringComparison.OrdinalIgnoreCase));
                if (duplicatePhoneLedger is not null)
                {
                    await _toastNotification.ShowAsync("Error", $"Phone number '{_ledger.Phone}' is already associated with another ledger. Please use a different phone number.", ToastType.Error);
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_ledger.Email))
            {
                var duplicateEmailLedger = _ledgers.FirstOrDefault(_ => _.Email.Equals(_ledger.Email, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmailLedger is not null)
                {
                    await _toastNotification.ShowAsync("Error", $"Email '{_ledger.Email}' is already associated with another ledger. Please use a different email address.", ToastType.Error);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task SaveLedger()
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

            await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

            if (_ledger.Id == 0)
                _ledger.Code = await GenerateCodes.GenerateLedgerCode();

            await LedgerData.InsertLedger(_ledger);

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

    private void ResetPage() =>
        NavigationManager.NavigateTo(PageRouteNames.LedgerMaster, true);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

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