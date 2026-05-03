using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.Reports;

public partial class KitchenProductionItemReport : IAsyncDisposable
{
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showSummary = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel? _selectedCompany = null;
    private KitchenModel? _selectedKitchen = null;

    private List<CompanyModel> _companies = [];
    private List<KitchenModel> _kitchens = [];
    private List<KitchenProductionItemOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
    ];

    private SfGrid<KitchenProductionItemOverviewModel> _sfGrid;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
        await LoadData();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadCompanies();
        await LoadKitchens();
        await LoadTransactionOverviews();
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
        _companies = [.. _companies.OrderBy(s => s.Name)];
    }

    private async Task LoadKitchens()
    {
        _kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
        _kitchens = [.. _kitchens.OrderBy(s => s.Name)];
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

            _transactionOverviews = await CommonData.LoadTableDataByDate<KitchenProductionItemOverviewModel>(
                ViewNames.KitchenProductionItemOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedKitchen?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.KitchenId == _selectedKitchen.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.ItemName)
                    .Select(g => new KitchenProductionItemOverviewModel
                    {
                        ItemName = g.Key,
                        ItemCode = g.First().ItemCode,
                        ItemCategoryName = g.First().ItemCategoryName,
                        Quantity = g.Sum(t => t.Quantity),
                        Total = g.Sum(t => t.Total)
                    })
                    .OrderBy(t => t.ItemName)];
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

    private async Task OnKitchenChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<KitchenModel, KitchenModel> args)
    {
        _selectedKitchen = args.Value;
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
            await _toastNotification.ShowAsync("Processing", "Generating Excel file...", ToastType.Info);
            var (stream, fileName) = await KitchenProductionReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.Excel,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedKitchen?.Id > 0 ? new KitchenModel { Id = _selectedKitchen.Id, Name = _selectedKitchen.Name } : null,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Excel file downloaded successfully.", ToastType.Success);
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
            await _toastNotification.ShowAsync("Processing", "Generating PDF file...", ToastType.Info);
            var (stream, fileName) = await KitchenProductionReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.PDF,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedKitchen?.Id > 0 ? new KitchenModel { Id = _selectedKitchen.Id, Name = _selectedKitchen.Name } : null,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null
                );

            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "PDF file downloaded successfully.", ToastType.Success);
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

    private async Task DownloadSelectedPdfInvoice()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, true, false, CodeType.KitchenProduction);
            await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

            await _toastNotification.ShowAsync("Success", "PDF invoice generated successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while generating invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DownloadSelectedExcelInvoice()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, true, CodeType.KitchenProduction);
            await SaveAndViewService.SaveAndView(decodeTransactionNo.ExcelStream.fileName, decodeTransactionNo.ExcelStream.stream);

            await _toastNotification.ShowAsync("Success", "Excel invoice generated successfully.", ToastType.Success);
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
    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenProductionItemOverviewModel> args)
    {
        if (_showSummary)
            return;

        switch (args.Item.Id)
        {
            case "View":
                await ViewSelectedTransaction();
                break;

            case "ExportPDF":
                await DownloadSelectedPdfInvoice();
                break;

            case "ExportExcel":
                await DownloadSelectedExcelInvoice();
                break;
        }
    }

    private async Task ViewSelectedTransaction()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false, CodeType.KitchenProduction);

        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", decodedTransactionNo.PageRouteName, "_blank");
        else
            NavigationManager.NavigateTo(decodedTransactionNo.PageRouteName);
    }
    #endregion

    #region Utilities
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
            case "ToggleSummary":
                await ToggleSummary();
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
                await NavigateToTransactionHistory();
                break;
            case "ViewSelected":
                await ViewSelectedTransaction();
                break;
            case "DownloadSelectedPdf":
                await DownloadSelectedPdfInvoice();
                break;
            case "DownloadSelectedExcel":
                await DownloadSelectedExcelInvoice();
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

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }

    private async Task ToggleSummary()
    {
        _showSummary = !_showSummary;
        await LoadTransactionOverviews();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.KitchenProduction, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.KitchenProduction);
    }

    private async Task NavigateToTransactionHistory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.KitchenProductionReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.KitchenProductionReport);
    }

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

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
