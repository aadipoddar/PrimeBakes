using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class FinancialAccountingReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showDeleted = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private VoucherModel _selectedVoucher = new();

    private List<CompanyModel> _companies = [];
    private List<VoucherModel> _vouchers = [];
    private List<FinancialAccountingOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
        new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
    ];

    private SfGrid<FinancialAccountingOverviewModel> _sfGrid;
    private ToastNotification _toastNotification;

    private string _deleteTransactionNo = string.Empty;
    private int _deleteTransactionId = 0;
    private string _recoverTransactionNo = string.Empty;
    private int _recoverTransactionId = 0;

    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts, UserRoles.Reports], true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        LoadHotKeys();
        await LoadDates();
        await LoadCompanies();
        await LoadVouchers();
        await LoadTransactionOverviews();
        await StartAutoRefresh();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
    }

    private async Task LoadCompanies()
    {
        _companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
        _companies.Add(new()
        {
            Id = 0,
            Name = "All Companies"
        });
        _companies = [.. _companies.OrderBy(s => s.Name)];
        _selectedCompany = _companies.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadVouchers()
    {
        _vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher);
        _vouchers.Add(new()
        {
            Id = 0,
            Name = "All Vouchers"
        });
        _vouchers = [.. _vouchers.OrderBy(s => s.Name)];
        _selectedVoucher = _vouchers.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadTransactionOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

            _transactionOverviews = await CommonData.LoadTableDataByDate<FinancialAccountingOverviewModel>(
                ViewNames.FinancialAccountingOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (!_showDeleted)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedVoucher?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.VoucherId == _selectedVoucher.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (_sfGrid is not null)
                await _sfGrid.Refresh();
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Changed Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadTransactionOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<VoucherModel, VoucherModel> args)
    {
        _selectedVoucher = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task HandleDatesChanged(DateRangeType dateRangeType)
    {
        (_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
        await LoadTransactionOverviews();
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
            await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);
            var (stream, fileName) = await FinancialAccountingReportExport.ExportReport(
                _transactionOverviews,
                ReportExportType.Excel,
                DateOnly.FromDateTime(_fromDate),
                DateOnly.FromDateTime(_toDate),
                _showAllColumns,
                _selectedCompany?.Id > 0 ? _selectedCompany : null,
                _selectedVoucher?.Id > 0 ? _selectedVoucher : null
            );

            await SaveAndViewService.SaveAndView(fileName, stream);
            await _toastNotification.ShowAsync("Exported", "Excel file downloaded successfully.", ToastType.Success);
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
            await _toastNotification.ShowAsync("Exporting", "Generating PDF file...", ToastType.Info);
            var (stream, fileName) = await FinancialAccountingReportExport.ExportReport(
                 _transactionOverviews,
                 ReportExportType.PDF,
                 DateOnly.FromDateTime(_fromDate),
                 DateOnly.FromDateTime(_toDate),
                 _showAllColumns,
                 _selectedCompany?.Id > 0 ? _selectedCompany : null,
                 _selectedVoucher?.Id > 0 ? _selectedVoucher : null
             );

            await SaveAndViewService.SaveAndView(fileName, stream);
            await _toastNotification.ShowAsync("Exported", "PDF file downloaded successfully.", ToastType.Success);
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

    private async Task ExportSelectedTransactionPdf()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo);
            await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

            await _toastNotification.ShowAsync("Success", "PDF invoice downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while generating PDF invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ExportSelectedTransactionExcel()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo);
            await SaveAndViewService.SaveAndView(decodeTransactionNo.ExcelStream.fileName, decodeTransactionNo.ExcelStream.stream);

            await _toastNotification.ShowAsync("Success", "Excel invoice downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while generating Excel invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Actions
    private async Task ViewSelectedTransaction()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || !_sfGrid.SelectedRecords.First().Status)
            return;

        var decodedTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo);

        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", decodedTransactionNo.PageRouteName, "_blank");
        else
            NavigationManager.NavigateTo(decodedTransactionNo.PageRouteName);
    }

    private async Task DeleteRecoverSelectedTransaction()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        if (_sfGrid.SelectedRecords.First().Status)
            await ShowDeleteConfirmation();
        else
            await ShowRecoverConfirmation();
    }

    private async Task ConfirmDelete()
    {
        if (_isProcessing || _deleteTransactionId == 0)
            return;

        try
        {
            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

            await _deleteConfirmationDialog.HideAsync();
            _isProcessing = true;
            StateHasChanged();

            await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

            var accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, _deleteTransactionId)
                ?? throw new Exception("Transaction not found.");
            accounting.Status = false;
            accounting.LastModifiedBy = _user.Id;
            accounting.LastModifiedAt = await CommonData.LoadCurrentDateTime();
            accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            await FinancialAccountingData.DeleteTransaction(accounting);

            await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);

            _deleteTransactionId = 0;
            _deleteTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadTransactionOverviews();
        }
    }

    private async Task ConfirmRecover()
    {
        if (_isProcessing || _recoverTransactionId == 0)
            return;

        try
        {
            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

            await _recoverConfirmationDialog.HideAsync();
            _isProcessing = true;
            StateHasChanged();

            await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

            var accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, _recoverTransactionId)
                ?? throw new Exception("Transaction not found.");
            accounting.Status = true;
            accounting.LastModifiedBy = _user.Id;
            accounting.LastModifiedAt = await CommonData.LoadCurrentDateTime();
            accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            await FinancialAccountingData.RecoverTransaction(accounting);

            await _toastNotification.ShowAsync("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", ToastType.Success);

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while recovering transaction: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadTransactionOverviews();
        }
    }
    #endregion

    #region Utilities
    private async Task ShowDeleteConfirmation()
    {
        _deleteTransactionId = _sfGrid.SelectedRecords.First().Id;
        _deleteTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
        StateHasChanged();
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteTransactionId = 0;
        _deleteTransactionNo = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ShowRecoverConfirmation()
    {
        _recoverTransactionId = _sfGrid.SelectedRecords.First().Id;
        _recoverTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
        StateHasChanged();
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
        await _recoverConfirmationDialog.HideAsync();
    }

    private void LoadHotKeys()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.Q, ToggleDetailsView, "Toggle Details", Exclude.None)
            .Add(ModCode.Ctrl, Code.Delete, ToggleDeleted, "Show/Hide Deleted", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Alt, Code.O, ViewSelectedTransaction, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, ExportSelectedTransactionPdf, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, ExportSelectedTransactionExcel, "Download Selected Transaction Excel Invoice", Exclude.None)
            .Add(Code.Delete, DeleteRecoverSelectedTransaction, "Delete Selected Transaction", Exclude.None);
    }

    private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
    {
        switch (args.Item.Id)
        {
            case "NewTransaction":
                await NavigateToTransactionPage();
                break;
            case "Refresh":
                await LoadTransactionOverviews();
                break;
            case "ToggleDeleted":
                await ToggleDeleted();
                break;
            case "ToggleDetailsView":
                await ToggleDetailsView();
                break;
            case "ExportPdf":
                await ExportPdf();
                break;
            case "ExportExcel":
                await ExportExcel();
                break;
            case "ViewSelected":
                await ViewSelectedTransaction();
                break;
            case "DownloadSelectedPdf":
                await ExportSelectedTransactionPdf();
                break;
            case "DownloadSelectedExcel":
                await ExportSelectedTransactionExcel();
                break;
            case "DeleteRecoverSelected":
                await DeleteRecoverSelectedTransaction();
                break;
            case "ItemReport":
                await NavigateToItemReport();
                break;
            case "TrialBalance":
                await NavigateToTrialBalance();
                break;
            case "ProfitLoss":
                await NavigateToProfitAndLoss();
                break;
            case "BalanceSheet":
                await NavigateToBalanceSheet();
                break;
            case "PeriodToday":
                await HandleDatesChanged(DateRangeType.Today);
                break;
            case "PeriodPreviousDay":
                await HandleDatesChanged(DateRangeType.Yesterday);
                break;
            case "PeriodNextDay":
                await HandleDatesChanged(DateRangeType.NextDay);
                break;
            case "PeriodCurrentMonth":
                await HandleDatesChanged(DateRangeType.CurrentMonth);
                break;
            case "PeriodPreviousMonth":
                await HandleDatesChanged(DateRangeType.PreviousMonth);
                break;
            case "PeriodNextMonth":
                await HandleDatesChanged(DateRangeType.NextMonth);
                break;
            case "PeriodCurrentFinancialYear":
                await HandleDatesChanged(DateRangeType.CurrentFinancialYear);
                break;
            case "PeriodPreviousFinancialYear":
                await HandleDatesChanged(DateRangeType.PreviousFinancialYear);
                break;
            case "PeriodNextFinancialYear":
                await HandleDatesChanged(DateRangeType.NextFinancialYear);
                break;
            case "PeriodAllTime":
                await HandleDatesChanged(DateRangeType.AllTime);
                break;
        }
    }

    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingOverviewModel> args)
    {
        switch (args.Item.Id)
        {
            case "View":
                await ViewSelectedTransaction();
                break;

            case "ExportPDF":
                await ExportSelectedTransactionPdf();
                break;

            case "ExportExcel":
                await ExportSelectedTransactionExcel();
                break;

            case "DeleteRecover":
                await DeleteRecoverSelectedTransaction();
                break;
        }
    }


    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }

    private async Task ToggleDeleted()
    {
        if (_user.LocationId > 1)
            return;

        _showDeleted = !_showDeleted;
        await LoadTransactionOverviews();
        StateHasChanged();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.FinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AccountingLedgerReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AccountingLedgerReport);
    }

    private async Task NavigateToTrialBalance()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.TrialBalanceReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.TrialBalanceReport);
    }

    private async Task NavigateToProfitAndLoss()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ProfitAndLossReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ProfitAndLossReport);
    }

    private async Task NavigateToBalanceSheet()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.BalanceSheetReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.BalanceSheetReport);
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

    private async Task StartAutoRefresh()
    {
        var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
        var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 5;

        _autoRefreshCts = new CancellationTokenSource();
        _autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
        _ = AutoRefreshLoop(_autoRefreshCts.Token);
    }

    private async Task AutoRefreshLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (await _autoRefreshTimer.WaitForNextTickAsync(cancellationToken))
                await LoadTransactionOverviews();
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, expected on dispose
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_autoRefreshCts is not null)
        {
            await _autoRefreshCts.CancelAsync();
            _autoRefreshCts.Dispose();
        }

        _autoRefreshTimer?.Dispose();

        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();

        GC.SuppressFinalize(this);
    }
    #endregion
}
