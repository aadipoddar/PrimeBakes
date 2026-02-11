using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class ProfitAndLossPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
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
    private List<TrialBalanceModel> _incomeTrialBalance = [];
    private List<TrialBalanceModel> _expenseTrialBalance = [];

    private SfGrid<TrialBalanceModel> _incomeGrid;
    private SfGrid<TrialBalanceModel> _expenseGrid;
    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts, UserRoles.Reports], true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.R, LoadProfitAndLoss, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadProfitAndLoss, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.I, NavigateToLedgerReport, "Ledger Report", Exclude.None)
            .Add(ModCode.Ctrl, Code.T, NavigateToTrialBalance, "Trial Balance Report", Exclude.None)
            .Add(ModCode.Alt, Code.B, NavigateToBalanceSheet, "Open balance sheet report", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None);

        await LoadDates();
        await LoadCompanies();
        await LoadProfitAndLoss();
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

    private async Task LoadProfitAndLoss()
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
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            _trialBalance = [.. _trialBalance.OrderBy(_ => _.LedgerName)];

            _incomeTrialBalance = [.. _trialBalance.Where(_ => _.NatureName == "Income")];
            _expenseTrialBalance = [.. _trialBalance.Where(_ => _.NatureName == "Expenses")];
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (_incomeGrid is not null)
                await _incomeGrid.Refresh();

            if (_expenseGrid is not null)
                await _expenseGrid.Refresh();

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
        await LoadProfitAndLoss();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadProfitAndLoss();
    }

    private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
    {
        _fromDate = dates.FromDate;
        _toDate = dates.ToDate;
        await LoadProfitAndLoss();
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

            // Export Income Statement
            var (incomeStream, incomeFileName) = await ProfitAndLossReportExport.ExportIncomeReport(
                    _incomeTrialBalance,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(incomeFileName, incomeStream);

            // Export Expense Statement
            var (expenseStream, expenseFileName) = await ProfitAndLossReportExport.ExportExpenseReport(
                    _expenseTrialBalance,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(expenseFileName, expenseStream);
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

            // Export Income Statement
            var (incomeStream, incomeFileName) = await ProfitAndLossReportExport.ExportIncomeReport(
                    _incomeTrialBalance,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(incomeFileName, incomeStream);

            // Export Expense Statement
            var (expenseStream, expenseFileName) = await ProfitAndLossReportExport.ExportExpenseReport(
                    _expenseTrialBalance,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(expenseFileName, expenseStream);
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
    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_incomeGrid is not null)
            await _incomeGrid.Refresh();
        if (_expenseGrid is not null)
            await _expenseGrid.Refresh();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.FinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting);
    }

    private async Task NavigateToLedgerReport()
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
                await LoadProfitAndLoss();
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