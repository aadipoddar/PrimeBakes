using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class LocationPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private LocationModel _location = new();
    private LocationModel _copyLocation;
    private LedgerModel _selectedLedger;

    private List<LocationModel> _locations = [];
    private List<LedgerModel> _ledgers = [];

    private SfGrid<LocationModel> _sfGrid;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteLocationId = 0;
    private string _deleteLocationName = string.Empty;

    private int _recoverLocationId = 0;
    private string _recoverLocationName = string.Empty;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveLocation, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
        _ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);

        if (!_showDeleted)
            _locations = [.. _locations.Where(l => l.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnLedgerChange(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        if (args.ItemData != null)
            _location.LedgerId = args.ItemData.Id;
        else
            _location.LedgerId = 0;
    }

    private void OnEditLocation(LocationModel location)
    {
        _location = new()
        {
            Id = location.Id,
            Name = location.Name,
            Code = location.Code,
            Discount = location.Discount,
            LedgerId = location.LedgerId,
            Remarks = location.Remarks,
            Status = location.Status
        };

        _selectedLedger = _ledgers.FirstOrDefault(l => l.Id == location.LedgerId);

        StateHasChanged();
    }

    private async Task ShowDeleteConfirmation(int id, string name)
    {
        _deleteLocationId = id;
        _deleteLocationName = name;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteLocationId = 0;
        _deleteLocationName = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            var location = _locations.FirstOrDefault(l => l.Id == _deleteLocationId);
            if (location == null)
            {
                await _toastNotification.ShowAsync("Error", "Location not found.", ToastType.Error);
                return;
            }

            if (location.Id == 1 && location.Status)
            {
                await _toastNotification.ShowAsync("Error", "Cannot delete main location.", ToastType.Error);
                return;
            }

            location.Status = false;
            await LocationData.InsertLocation(location);

            await _toastNotification.ShowAsync("Deleted", $"Location '{location.Name}' removed successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.Location, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete location: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteLocationId = 0;
            _deleteLocationName = string.Empty;
        }
    }

    private async Task ShowRecoverConfirmation(int id, string name)
    {
        _recoverLocationId = id;
        _recoverLocationName = name;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverLocationId = 0;
        _recoverLocationName = string.Empty;
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

            var location = _locations.FirstOrDefault(l => l.Id == _recoverLocationId);
            if (location == null)
            {
                await _toastNotification.ShowAsync("Error", "Location not found.", ToastType.Error);
                return;
            }

            location.Status = true;
            await LocationData.InsertLocation(location);

            await _toastNotification.ShowAsync("Recovered", $"Location '{location.Name}' restored successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.Location, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover location: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverLocationId = 0;
            _recoverLocationName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _location.Name = _location.Name?.Trim() ?? "";
        _location.Code = _location.Code?.Trim() ?? "";

        _location.Name = _location.Name?.ToUpper() ?? "";
        _location.Code = _location.Code?.ToUpper() ?? "";

        _location.Remarks = _location.Remarks?.Trim() ?? "";
        _location.Status = true;

        if (string.IsNullOrWhiteSpace(_location.Name))
        {
            await _toastNotification.ShowAsync("Validation", "Location name is required.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_location.Code))
        {
            await _toastNotification.ShowAsync("Validation", "Code is required.", ToastType.Warning);
            return false;
        }

        if (_location.LedgerId <= 0)
        {
            await _toastNotification.ShowAsync("Validation", "Ledger is required.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_location.Remarks))
            _location.Remarks = null;

        if (_location.Id > 0)
        {
            var existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.Code.Equals(_location.Code, StringComparison.OrdinalIgnoreCase));
            if (existingLocation is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Code '{_location.Code}' already exists.", ToastType.Warning);
                return false;
            }

            existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLocation is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Location name '{_location.Name}' already exists.", ToastType.Warning);
                return false;
            }
        }
        else
        {
            var existingLocation = _locations.FirstOrDefault(_ => _.Code.Equals(_location.Code, StringComparison.OrdinalIgnoreCase));
            if (existingLocation is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Code '{_location.Code}' already exists.", ToastType.Warning);
                return false;
            }

            existingLocation = _locations.FirstOrDefault(_ => _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLocation is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Location name '{_location.Name}' already exists.", ToastType.Warning);
                return false;
            }
        }

        if (_location.Discount is < 0 or > 100)
        {
            await _toastNotification.ShowAsync("Validation", "Discount must be between 0% and 100%.", ToastType.Warning);
            return false;
        }

        return true;
    }

    private async Task SaveLocation()
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

            await _toastNotification.ShowAsync("Saving", "Processing location...", ToastType.Info);

            _location.Id = await LocationData.SaveTransaction(_location, _copyLocation);

            await _toastNotification.ShowAsync("Saved", $"Location '{_location.Name}' saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.Location, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save location: {ex.Message}", ToastType.Error);
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

            var (stream, fileName) = await LocationExport.ExportMaster(_locations, ReportExportType.Excel);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Location data exported to Excel successfully.", ToastType.Success);
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

            var (stream, fileName) = await LocationExport.ExportMaster(_locations, ReportExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Location data exported to PDF successfully.", ToastType.Success);
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
            OnEditLocation(selectedRecords[0]);
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
        NavigationManager.NavigateTo(PageRouteNames.Location, true);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.AdminDashboard);

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