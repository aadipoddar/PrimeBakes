using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase.Reports;

public partial class PurchaseReturnItemReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showSummary = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<PurchaseReturnItemOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
    ];

    private SfGrid<PurchaseReturnItemOverviewModel> _sfGrid;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        LoadHotKeys();

        await LoadDates();
        await LoadCompanies();
        await LoadParties();
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

    private async Task LoadParties()
    {
        _parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
        _parties.Add(new()
        {
            Id = 0,
            Name = "All Parties"
        });
        _parties = [.. _parties.OrderBy(s => s.Name)];
        _selectedParty = _parties.FirstOrDefault(_ => _.Id == 0);
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

            _transactionOverviews = await CommonData.LoadTableDataByDate<PurchaseReturnItemOverviewModel>(
                ViewNames.PurchaseReturnItemOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue)
            );

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.ItemName)
                    .Select(g => new PurchaseReturnItemOverviewModel
                    {
                        ItemName = g.Key,
                        ItemCode = g.First().ItemCode,
                        ItemCategoryName = g.First().ItemCategoryName,
                        Quantity = g.Sum(t => t.Quantity),
                        BaseTotal = g.Sum(t => t.BaseTotal),
                        DiscountAmount = g.Sum(t => t.DiscountAmount),
                        AfterDiscount = g.Sum(t => t.AfterDiscount),
                        SGSTAmount = g.Sum(t => t.SGSTAmount),
                        CGSTAmount = g.Sum(t => t.CGSTAmount),
                        IGSTAmount = g.Sum(t => t.IGSTAmount),
                        TotalTaxAmount = g.Sum(t => t.TotalTaxAmount),
                        Total = g.Sum(t => t.Total),
                        NetTotal = g.Sum(t => t.NetTotal)
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

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        _selectedParty = args.Value;
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
            var (stream, fileName) = await PurchaseReturnReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.Excel,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedParty?.Id > 0 ? _selectedParty : null,
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
            var (stream, fileName) = await PurchaseReturnReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.PDF,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedParty?.Id > 0 ? _selectedParty : null,
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

            var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().ChallanNo);
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

    private async Task DownloadSelectedExcelInvoice()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().ChallanNo);
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
    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<PurchaseReturnItemOverviewModel> args)
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

        var decodedTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().ChallanNo);

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
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.W, ToggleSummary, "Show/Hide Summary", Exclude.None)
            .Add(ModCode.Ctrl, Code.Q, ToggleDetailsView, "Toggle Details", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistory, "Open transaction history", Exclude.None)
            .Add(ModCode.Ctrl, Code.O, ViewSelectedTransaction, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, DownloadSelectedPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, DownloadSelectedExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None);
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
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.PurchaseReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.PurchaseReturn);
    }

    private async Task NavigateToTransactionHistory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.PurchaseReturnReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.PurchaseReturnReport);
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

    public ValueTask DisposeAsync()
    {
        if (_autoRefreshCts is not null)
        {
            _autoRefreshCts.CancelAsync();
            _autoRefreshCts.Dispose();
        }

        _autoRefreshTimer?.Dispose();

        GC.SuppressFinalize(this);
        return ((IAsyncDisposable)HotKeys).DisposeAsync();
    }
    #endregion
}

