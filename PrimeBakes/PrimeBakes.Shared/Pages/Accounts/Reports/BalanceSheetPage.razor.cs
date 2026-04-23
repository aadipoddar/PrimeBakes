using Microsoft.AspNetCore.Components;

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

public partial class BalanceSheetPage : IAsyncDisposable
{
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();

    private List<CompanyModel> _companies = [];
    private List<TrialBalanceModel> _trialBalance = [];
    private List<TrialBalanceModel> _assetsTrialBalance = [];
    private List<TrialBalanceModel> _liabilitiesTrialBalance = [];

    private SfGrid<TrialBalanceModel> _assetsGrid;
    private SfGrid<TrialBalanceModel> _liabilitiesGrid;
    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts, UserRoles.Reports], true);
        await LoadData();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadCompanies();
        await LoadBalanceSheet();
        await StartAutoRefresh();

        _isLoading = false;
        StateHasChanged();
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

    private async Task LoadBalanceSheet()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

            _trialBalance = await FinancialAccountingData.LoadTrialBalanceByCompanyDate(
                _selectedCompany?.Id ?? 0,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            _trialBalance = [.. _trialBalance.OrderBy(_ => _.LedgerName)];

            _assetsTrialBalance = [.. _trialBalance.Where(_ => _.NatureName == "Assets")];
            _liabilitiesTrialBalance = [.. _trialBalance.Where(_ => _.NatureName == "Liabilities")];
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (_assetsGrid is not null)
                await _assetsGrid.Refresh();

            if (_liabilitiesGrid is not null)
                await _liabilitiesGrid.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Change Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadBalanceSheet();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadBalanceSheet();
    }

    private async Task HandleDatesChanged(DateRangeType dateRangeType)
    {
        (_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
        await LoadBalanceSheet();
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
            await _toastNotification.ShowAsync("Processing", "Generating Excel files...", ToastType.Info);

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            // Export Assets Statement
            var (assetsStream, assetsFileName) = await BalanceSheetReportExport.ExportAssetsReport(
                    _assetsTrialBalance,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(assetsFileName, assetsStream);

            // Export Liabilities Statement
            var (liabilitiesStream, liabilitiesFileName) = await BalanceSheetReportExport.ExportLiabilitiesReport(
                    _liabilitiesTrialBalance,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(liabilitiesFileName, liabilitiesStream);
            await _toastNotification.ShowAsync("Success", "Excel files downloaded successfully.", ToastType.Success);
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
            await _toastNotification.ShowAsync("Processing", "Generating PDF files...", ToastType.Info);

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            // Export Assets Statement
            var (assetsStream, assetsFileName) = await BalanceSheetReportExport.ExportAssetsReport(
                    _assetsTrialBalance,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(assetsFileName, assetsStream);

            // Export Liabilities Statement
            var (liabilitiesStream, liabilitiesFileName) = await BalanceSheetReportExport.ExportLiabilitiesReport(
                    _liabilitiesTrialBalance,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(liabilitiesFileName, liabilitiesStream);
            await _toastNotification.ShowAsync("Success", "PDF files downloaded successfully.", ToastType.Success);
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

    private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
    {
        switch (args.Item.Id)
        {
            case "NewTransaction":
                await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccounting, FormFactor, JSRuntime, NavigationManager);
                break;
            case "Refresh":
                await LoadBalanceSheet();
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
            case "TransactionHistory":
                await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "LedgerReport":
                await AuthenticationService.NavigateToRoute(PageRouteNames.AccountingLedgerReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "TrialBalance":
                await AuthenticationService.NavigateToRoute(PageRouteNames.TrialBalanceReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "ProfitLoss":
                await AuthenticationService.NavigateToRoute(PageRouteNames.ProfitAndLossReport, FormFactor, JSRuntime, NavigationManager);
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

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_assetsGrid is not null)
            await _assetsGrid.Refresh();
        if (_liabilitiesGrid is not null)
            await _liabilitiesGrid.Refresh();
    }

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

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
                await LoadBalanceSheet();
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, expected on dispose
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_autoRefreshCts is not null)
        {
            await _autoRefreshCts.CancelAsync();
            _autoRefreshCts.Dispose();
        }

        _autoRefreshTimer?.Dispose();

        GC.SuppressFinalize(this);
    }
    #endregion
}
