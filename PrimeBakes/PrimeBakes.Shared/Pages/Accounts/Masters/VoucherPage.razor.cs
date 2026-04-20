using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts.Masters;

public partial class VoucherPage : IAsyncDisposable
{
    private UserModel _user;
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private VoucherModel _voucher = new();

    private List<VoucherModel> _vouchers = [];
    private readonly List<ContextMenuItemModel> _voucherGridContextMenuItems =
    [
        new() { Text = "Edit (Insert)", Id = "EditVoucher", IconCss = "e-icons e-edit", Target = ".e-content" },
        new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverVoucher", IconCss = "e-icons e-trash", Target = ".e-content" }
    ];

    private SfGrid<VoucherModel> _sfGrid;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteVoucherId = 0;
    private string _deleteVoucherName = string.Empty;

    private int _recoverVoucherId = 0;
    private string _recoverVoucherName = string.Empty;

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
            .Add(ModCode.Ctrl, Code.S, SaveVoucher, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
            .Add(ModCode.Ctrl, Code.Delete, ToggleDeleted, "Show/Hide Deleted", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete / Recover selected", Exclude.None);

        _vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);

        if (!_showDeleted)
            _vouchers = [.. _vouchers.Where(v => v.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditVoucher(VoucherModel voucher)
    {
        _voucher = new()
        {
            Id = voucher.Id,
            Name = voucher.Name,
            Code = voucher.Code,
            Remarks = voucher.Remarks,
            Status = voucher.Status
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

            var voucher = _vouchers.FirstOrDefault(v => v.Id == _deleteVoucherId)
                ?? throw new Exception("Voucher not found.");

            voucher.Status = false;
            await VoucherData.InsertVoucher(voucher);

            await _toastNotification.ShowAsync("Success", $"Voucher '{voucher.Name}' has been deleted successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.VoucherMaster, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete Voucher: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteVoucherId = 0;
            _deleteVoucherName = string.Empty;
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

            var voucher = _vouchers.FirstOrDefault(v => v.Id == _recoverVoucherId)
                ?? throw new Exception("Voucher not found.");

            voucher.Status = true;
            await VoucherData.InsertVoucher(voucher);

            await _toastNotification.ShowAsync("Success", $"Voucher '{voucher.Name}' has been recovered successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.VoucherMaster, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover Voucher: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverVoucherId = 0;
            _recoverVoucherName = string.Empty;
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

        _voucher.Name = _voucher.Name?.Trim() ?? "";
        _voucher.Name = _voucher.Name?.ToUpper() ?? "";

        _voucher.Code = _voucher.Code?.Trim() ?? "";
        _voucher.Code = _voucher.Code?.ToUpper() ?? "";

        _voucher.Remarks = _voucher.Remarks?.Trim() ?? "";
        _voucher.Status = true;

        if (string.IsNullOrWhiteSpace(_voucher.Name))
        {
            await _toastNotification.ShowAsync("Validation", "Voucher name is required. Please enter a valid voucher name.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_voucher.Code))
        {
            await _toastNotification.ShowAsync("Validation", "Code is required. Please enter a valid code.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_voucher.Remarks))
            _voucher.Remarks = null;

        if (_voucher.Id > 0)
        {
            var existingVoucher = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
            if (existingVoucher is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }

            var existingCode = _vouchers.FirstOrDefault(_ => _.Id != _voucher.Id && _.Code.Equals(_voucher.Code, StringComparison.OrdinalIgnoreCase));
            if (existingCode is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Code '{_voucher.Code}' already exists. Please choose a different code.", ToastType.Error);
                return false;
            }
        }
        else
        {
            var existingVoucher = _vouchers.FirstOrDefault(_ => _.Name.Equals(_voucher.Name, StringComparison.OrdinalIgnoreCase));
            if (existingVoucher is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Voucher name '{_voucher.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }

            var existingCode = _vouchers.FirstOrDefault(_ => _.Code.Equals(_voucher.Code, StringComparison.OrdinalIgnoreCase));
            if (existingCode is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Code '{_voucher.Code}' already exists. Please choose a different code.", ToastType.Error);
                return false;
            }
        }

        return true;
    }

    private async Task SaveVoucher()
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

            await VoucherData.InsertVoucher(_voucher);

            await _toastNotification.ShowAsync("Saved", $"Voucher '{_voucher.Name}' has been saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.VoucherMaster, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save Voucher: {ex.Message}", ToastType.Error);
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

            var (stream, fileName) = await VoucherExport.ExportMaster(_vouchers, ReportExportType.Excel);
            await SaveAndViewService.SaveAndView(fileName, stream);
            await _toastNotification.ShowAsync("Success", "Voucher data exported to Excel successfully.", ToastType.Success);
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

            var (stream, fileName) = await VoucherExport.ExportMaster(_vouchers, ReportExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, stream);
            await _toastNotification.ShowAsync("Success", "Voucher data exported to PDF successfully.", ToastType.Success);
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
            case "NewVoucher":
                ResetPage();
                break;
            case "SaveVoucher":
                await SaveVoucher();
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

    private async Task OnVoucherGridContextMenuItemClicked(ContextMenuClickEventArgs<VoucherModel> args)
    {
        switch (args.Item.Id)
        {
            case "EditVoucher":
                await EditSelectedItem();
                break;
            case "DeleteRecoverVoucher":
                await DeleteSelectedItem();
                break;
        }
    }

    private async Task EditSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
            OnEditVoucher(selectedRecords[0]);
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

    private async Task ShowDeleteConfirmation(int id, string name)
    {
        _deleteVoucherId = id;
        _deleteVoucherName = name;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteVoucherId = 0;
        _deleteVoucherName = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ShowRecoverConfirmation(int id, string name)
    {
        _recoverVoucherId = id;
        _recoverVoucherName = name;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverVoucherId = 0;
        _recoverVoucherName = string.Empty;
        await _recoverConfirmationDialog.HideAsync();
    }

    private async Task ToggleDeleted()
    {
        _showDeleted = !_showDeleted;
        await LoadData();
        StateHasChanged();
    }

    private void ResetPage() =>
        NavigationManager.NavigateTo(PageRouteNames.VoucherMaster, true);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ((IAsyncDisposable)HotKeys).DisposeAsync();
    }
    #endregion
}
