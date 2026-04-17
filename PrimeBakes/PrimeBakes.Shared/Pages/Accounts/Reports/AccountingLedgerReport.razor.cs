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

public partial class AccountingLedgerReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedLedger = new();
    private TrialBalanceModel _selectedTrialBalance = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _ledgers = [];
    private List<FinancialAccountingLedgerOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
    ];

    private SfGrid<FinancialAccountingLedgerOverviewModel> _sfGrid;
    private ToastNotification _toastNotification;

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
        await LoadLedgers();
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

    private async Task LoadLedgers()
    {
        _ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
        _ledgers.Add(new()
        {
            Id = 0,
            Name = "All Ledgers"
        });

        _ledgers = [.. _ledgers.OrderBy(s => s.Name)];
        _selectedLedger = _ledgers.FirstOrDefault(_ => _.Id == 0);
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

            _transactionOverviews = await CommonData.LoadTableDataByDate<FinancialAccountingLedgerOverviewModel>(
                ViewNames.FinancialAccountingLedgerOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            // Filter by ledger with contra ledger details
            if (_selectedLedger?.Id > 0)
            {
                List<FinancialAccountingLedgerOverviewModel> filteredOverviews = [];
                var partyLedgers = _transactionOverviews.Where(l => l.Id == _selectedLedger.Id).ToList();

                foreach (var item in partyLedgers)
                {
                    var referenceLedgers = _transactionOverviews
                        .Where(l => l.MasterId == item.MasterId && l.Id != _selectedLedger.Id)
                        .ToList();

                    var referenceLedgerNamesWithAmount = string.Join("\n",
                        referenceLedgers.Select(l =>
                        $"{l.LedgerName}\t({(l.Debit is > 0 ? "Dr " + l.Debit.Value.FormatIndianCurrency() : l.Credit is > 0 ? "Cr " + l.Credit.Value.FormatIndianCurrency() : "0.00")})")); item.LedgerName = referenceLedgerNamesWithAmount;
                    filteredOverviews.Add(item);
                }

                _transactionOverviews = filteredOverviews;

                var trialBalances = await FinancialAccountingData.LoadTrialBalanceByCompanyDate(
                    _selectedCompany?.Id ?? 0,
                    DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                    DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

                _selectedTrialBalance = trialBalances.FirstOrDefault(tb => tb.LedgerId == _selectedLedger.Id);
            }

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

    #region Change Events
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

    private async Task OnLedgerChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        _selectedLedger = args.Value;
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
            var (stream, fileName) = await FinancialAccountingReportExport.ExportLedgerReport(
                _transactionOverviews,
                ReportExportType.Excel,
                DateOnly.FromDateTime(_fromDate),
                DateOnly.FromDateTime(_toDate),
                _showAllColumns,
                _selectedCompany?.Id > 0 ? _selectedCompany : null,
                _selectedLedger?.Id > 0 ? _selectedLedger : null,
                _selectedLedger?.Id > 0 ? _selectedTrialBalance : null
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
            var (stream, fileName) = await FinancialAccountingReportExport.ExportLedgerReport(
                _transactionOverviews,
                ReportExportType.PDF,
                DateOnly.FromDateTime(_fromDate),
                DateOnly.FromDateTime(_toDate),
                _showAllColumns,
                _selectedCompany?.Id > 0 ? _selectedCompany : null,
                _selectedLedger?.Id > 0 ? _selectedLedger : null,
                _selectedLedger?.Id > 0 ? _selectedTrialBalance : null
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
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var decodedTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo);

        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", decodedTransactionNo.PageRouteName, "_blank");
        else
            NavigationManager.NavigateTo(decodedTransactionNo.PageRouteName);
    }
    #endregion

    #region Utilities
    private void LoadHotKeys()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccounting, FormFactor, JSRuntime, NavigationManager), "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.Q, ToggleDetailsView, "Toggle Details", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Alt, Code.O, ViewSelectedTransaction, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, ExportSelectedTransactionPdf, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, ExportSelectedTransactionExcel, "Download Selected Transaction Excel Invoice", Exclude.None);
    }

    private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
    {
        switch (args.Item.Id)
        {
            case "NewTransaction":
                await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccounting, FormFactor, JSRuntime, NavigationManager);
                break;
            case "Refresh":
                await LoadTransactionOverviews();
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
            case "TransactionHistory":
                await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "TrialBalance":
                await AuthenticationService.NavigateToRoute(PageRouteNames.TrialBalanceReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "ProfitLoss":
                await AuthenticationService.NavigateToRoute(PageRouteNames.ProfitAndLossReport, FormFactor, JSRuntime, NavigationManager);
                break;
            case "BalanceSheet":
                await AuthenticationService.NavigateToRoute(PageRouteNames.BalanceSheetReport, FormFactor, JSRuntime, NavigationManager);
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

    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingLedgerOverviewModel> args)
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
        }
    }

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
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
