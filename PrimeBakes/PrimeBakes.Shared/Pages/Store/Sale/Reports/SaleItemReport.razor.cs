using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Sale;
using PrimeBakesLibrary.Models.Store.StockTransfer;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleItemReport : IAsyncDisposable
{
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showSaleReturns = false;
    private bool _showStockTransfers = false;
    private bool _showSummary = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel? _selectedLocation = null;
    private CompanyModel? _selectedCompany = null;
    private LedgerModel? _selectedParty = null;

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<SaleItemOverviewModel> _transactionOverviews = [];
    private List<SaleReturnItemOverviewModel> _transactionReturnOverviews = [];
    private List<StockTransferItemOverviewModel> _transactionTransferOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
    ];

    private SfGrid<SaleItemOverviewModel> _sfGrid;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store, UserRoles.Reports]);
        await LoadData();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadParties();
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

    private async Task LoadLocations()
    {
        _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        _locations = [.. _locations.OrderBy(s => s.Name)];
        _selectedLocation = _locations.FirstOrDefault(_ => _.Id == _user.LocationId);
    }

    private async Task LoadCompanies()
    {
        _companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
        _companies = [.. _companies.OrderBy(s => s.Name)];
    }

    private async Task LoadParties()
    {
        _parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
        _parties = [.. _parties.OrderBy(s => s.Name)];
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

            _transactionOverviews = await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(
                ViewNames.SaleItemOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (_selectedLocation?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSaleReturns)
                await LoadTransactionReturnOverviews();

            if (_showStockTransfers)
                await LoadTransactionTransferOverviews();

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.ItemName)
                    .Select(g => new SaleItemOverviewModel
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

    private async Task LoadTransactionReturnOverviews()
    {
        _transactionReturnOverviews = await CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(
            ViewNames.SaleReturnItemOverview,
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

        if (_selectedLocation?.Id > 0)
            _transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

        if (_selectedCompany?.Id > 0)
            _transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

        if (_selectedParty?.Id > 0)
            _transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

        _transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

        MergeTransactionAndReturns();
    }

    private void MergeTransactionAndReturns()
    {
        _transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new SaleItemOverviewModel
        {
            Id = -pr.Id,
            MasterId = -pr.MasterId,
            OrderTransactionNo = null,
            CustomerId = pr.CustomerId,
            CustomerName = pr.CustomerName,
            LocationId = pr.LocationId,
            LocationName = pr.LocationName,
            OrderId = null,
            SaleRemarks = pr.SaleReturnRemarks,
            ItemName = pr.ItemName,
            ItemCode = pr.ItemCode,
            ItemCategoryId = pr.ItemCategoryId,
            ItemCategoryName = pr.ItemCategoryName,
            CompanyId = pr.CompanyId,
            CompanyName = pr.CompanyName,
            PartyId = pr.PartyId,
            PartyName = pr.PartyName,
            TransactionNo = pr.TransactionNo,
            TransactionDateTime = pr.TransactionDateTime,
            Quantity = -pr.Quantity,
            Rate = pr.Rate,
            BaseTotal = -pr.BaseTotal,
            DiscountPercent = pr.DiscountPercent,
            DiscountAmount = -pr.DiscountAmount,
            AfterDiscount = -pr.AfterDiscount,
            CGSTPercent = pr.CGSTPercent,
            CGSTAmount = -pr.CGSTAmount,
            SGSTPercent = pr.SGSTPercent,
            SGSTAmount = -pr.SGSTAmount,
            IGSTPercent = pr.IGSTPercent,
            IGSTAmount = -pr.IGSTAmount,
            TotalTaxAmount = -pr.TotalTaxAmount,
            InclusiveTax = pr.InclusiveTax,
            Total = -pr.Total,
            NetRate = pr.NetRate,
            NetTotal = -pr.NetTotal,
            Remarks = pr.Remarks
        }));

        _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
    }

    private async Task LoadTransactionTransferOverviews()
    {
        _transactionTransferOverviews = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(
            ViewNames.StockTransferItemOverview,
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

        if (_selectedLocation?.Id > 0)
            _transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

        if (_selectedCompany?.Id > 0)
            _transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

        if (_selectedParty?.Id > 0)
        {
            var location = _locations.FirstOrDefault(l => l.LedgerId == _selectedParty.Id);
            if (location is not null)
                _transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.ToLocationId == location.Id)];
        }

        _transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

        MergeTransactionAndTransfers();
    }

    private void MergeTransactionAndTransfers()
    {
        _transactionOverviews.AddRange(_transactionTransferOverviews.Select(pr => new SaleItemOverviewModel
        {
            Id = 0,
            MasterId = 0,
            OrderTransactionNo = null,
            CustomerId = null,
            CustomerName = null,
            LocationId = pr.LocationId,
            LocationName = pr.LocationName,
            OrderId = null,
            SaleRemarks = pr.StockTransferRemarks,
            ItemName = pr.ItemName,
            ItemCode = pr.ItemCode,
            ItemCategoryId = pr.ItemCategoryId,
            ItemCategoryName = pr.ItemCategoryName,
            CompanyId = pr.CompanyId,
            CompanyName = pr.CompanyName,
            PartyId = _locations.FirstOrDefault(l => l.LedgerId == pr.ToLocationId)?.Id,
            PartyName = _locations.FirstOrDefault(l => l.LedgerId == pr.ToLocationId)?.Name,
            TransactionNo = pr.TransactionNo,
            TransactionDateTime = pr.TransactionDateTime,
            Quantity = pr.Quantity,
            Rate = pr.Rate,
            BaseTotal = pr.BaseTotal,
            DiscountPercent = pr.DiscountPercent,
            DiscountAmount = pr.DiscountAmount,
            AfterDiscount = pr.AfterDiscount,
            CGSTPercent = pr.CGSTPercent,
            CGSTAmount = pr.CGSTAmount,
            SGSTPercent = pr.SGSTPercent,
            SGSTAmount = pr.SGSTAmount,
            IGSTPercent = pr.IGSTPercent,
            IGSTAmount = pr.IGSTAmount,
            TotalTaxAmount = pr.TotalTaxAmount,
            InclusiveTax = pr.InclusiveTax,
            Total = pr.Total,
            NetRate = pr.NetRate,
            NetTotal = pr.NetTotal,
            Remarks = pr.Remarks
        }));

        _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
    }
    #endregion

    #region Change Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadTransactionOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        if (_user.LocationId > 1)
            return;

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
            var (stream, fileName) = await SaleReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.Excel,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedParty?.Id > 0 ? _selectedParty : null,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null,
                    _selectedLocation?.Id > 0 ? _selectedLocation : null
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
            var (stream, fileName) = await SaleReportExport.ExportItemReport(
                    _transactionOverviews,
                    ReportExportType.PDF,
                    DateOnly.FromDateTime(_fromDate),
                    DateOnly.FromDateTime(_toDate),
                    _showAllColumns,
                    _showSummary,
                    _selectedParty?.Id > 0 ? _selectedParty : null,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null,
                    _selectedLocation?.Id > 0 ? _selectedLocation : null
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
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrEmpty(_sfGrid.SelectedRecords.First().TransactionNo))
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, true, false);
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
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrEmpty(_sfGrid.SelectedRecords.First().TransactionNo))
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, true);
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
    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<SaleItemOverviewModel> args)
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

        try
        {
            var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false);
            await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
        }
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
            case "ToggleSaleReturns":
                await ToggleSaleReturns();
                break;
            case "ToggleStockTransfers":
                await ToggleStockTransfers();
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

    private async Task ToggleSaleReturns()
    {
        _showSaleReturns = !_showSaleReturns;
        await LoadTransactionOverviews();
    }

    private async Task ToggleStockTransfers()
    {
        _showStockTransfers = !_showStockTransfers;
        await LoadTransactionOverviews();
    }

    private async Task ToggleSummary()
    {
        _showSummary = !_showSummary;
        await LoadTransactionOverviews();
    }

    private async Task NavigateToTransactionPage() =>
        await AuthenticationService.NavigateToRoute(PageRouteNames.Sale, FormFactor, JSRuntime, NavigationManager);

    private async Task NavigateToTransactionHistory() =>
        await AuthenticationService.NavigateToRoute(PageRouteNames.SaleReport, FormFactor, JSRuntime, NavigationManager);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.StoreDashboard);

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
