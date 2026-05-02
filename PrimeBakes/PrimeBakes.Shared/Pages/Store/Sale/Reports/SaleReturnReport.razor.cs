using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Sale;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReturnReport : IAsyncDisposable
{
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showSummary = false;
    private bool _showDeleted = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel? _selectedLocation = null;
    private CompanyModel? _selectedCompany = null;
    private LedgerModel? _selectedParty = null;

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<SaleReturnOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
        new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
        new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
        new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
    ];

    private SfGrid<SaleReturnOverviewModel> _sfGrid;

    private string _deleteTransactionNo = string.Empty;
    private int _deleteTransactionId = 0;

    private string _recoverTransactionNo = string.Empty;
    private int _recoverTransactionId = 0;

    private ToastNotification _toastNotification;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

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

            _transactionOverviews = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(
                ViewNames.SaleReturnOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (!_showDeleted)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

            if (_selectedLocation?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.PartyName)
                    .Select(g => new SaleReturnOverviewModel
                    {
                        PartyName = g.Key,
                        TotalItems = g.Sum(t => t.TotalItems),
                        TotalQuantity = g.Sum(t => t.TotalQuantity),
                        BaseTotal = g.Sum(t => t.BaseTotal),
                        ItemDiscountAmount = g.Sum(t => t.ItemDiscountAmount),
                        TotalAfterItemDiscount = g.Sum(t => t.TotalAfterItemDiscount),
                        TotalInclusiveTaxAmount = g.Sum(t => t.TotalInclusiveTaxAmount),
                        TotalExtraTaxAmount = g.Sum(t => t.TotalExtraTaxAmount),
                        TotalAfterTax = g.Sum(t => t.TotalAfterTax),
                        OtherChargesAmount = g.Sum(t => t.OtherChargesAmount),
                        DiscountAmount = g.Sum(t => t.DiscountAmount),
                        RoundOffAmount = g.Sum(t => t.RoundOffAmount),
                        TotalAmount = g.Sum(t => t.TotalAmount),
                        Cash = g.Sum(t => t.Cash),
                        Card = g.Sum(t => t.Card),
                        UPI = g.Sum(t => t.UPI),
                        Credit = g.Sum(t => t.Credit)
                    })];
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
            var (stream, fileName) = await SaleReturnReportExport.ExportReport(
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

            // Convert DateTime to DateOnly for PDF export
            var (stream, fileName) = await SaleReturnReportExport.ExportReport(
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

    private async Task DownloadSelectedExcelInvoice()
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
    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnOverviewModel> args)
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

            case "DeleteRecover":
                await DeleteRecoverSelectedTransaction();
                break;
        }
    }

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

    private async Task DeleteRecoverSelectedTransaction()
    {
        if (!_user.Admin || _user.LocationId > 1)
            return;

        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();

        if (selectedCartItem.Status)
            await ShowDeleteConfirmation();
        else
            await ShowRecoverConfirmation();
    }

    private async Task ConfirmDelete()
    {
        if (_isProcessing)
            return;

        try
        {
            await _deleteConfirmationDialog.HideAsync();
            _isProcessing = true;
            StateHasChanged();

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to delete this sale return transaction.");

            if (_deleteTransactionId == 0)
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to delete.", ToastType.Error);
                return;
            }

            await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

            await DeleteTransaction();

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

    private async Task DeleteTransaction()
    {
        var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _deleteTransactionId);
        if (saleReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        saleReturn.Status = false;
        saleReturn.LastModifiedBy = _user.Id;
        saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleReturnData.DeleteTransaction(saleReturn);
    }

    private async Task ConfirmRecover()
    {
        if (_isProcessing)
            return;

        try
        {
            await _recoverConfirmationDialog.HideAsync();
            _isProcessing = true;
            StateHasChanged();

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

            if (_recoverTransactionId == 0)
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to recover.", ToastType.Error);
                return;
            }

            await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

            await RecoverTransaction();

            await _toastNotification.ShowAsync("Success", $"Transaction '{_recoverTransactionNo}' has been successfully recovered.", ToastType.Success);

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover sale return: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadTransactionOverviews();
        }
    }

    private async Task RecoverTransaction()
    {
        var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _recoverTransactionId);
        if (saleReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        saleReturn.Status = true;
        saleReturn.LastModifiedBy = _user.Id;
        saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleReturnData.RecoverTransaction(saleReturn);
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
            case "ToggleDeleted":
                await ToggleDeleted();
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
            case "ItemReport":
                await NavigateToItemReport();
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
            case "DeleteRecoverSelected":
                await DeleteRecoverSelectedTransaction();
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

    private async Task ShowDeleteConfirmation()
    {
        if (_user.LocationId > 1)
            return;

        _deleteTransactionId = _sfGrid.SelectedRecords.First().Id;
        _deleteTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
        StateHasChanged();
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        await _deleteConfirmationDialog.HideAsync();
        _deleteTransactionId = 0;
        _deleteTransactionNo = string.Empty;
    }

    private async Task ShowRecoverConfirmation()
    {
        if (_user.LocationId > 1)
            return;

        _recoverTransactionId = _sfGrid.SelectedRecords.First().Id;
        _recoverTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
        StateHasChanged();
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        await _recoverConfirmationDialog.HideAsync();
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
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
    }

    private async Task ToggleSummary()
    {
        if (_user.LocationId > 1)
            return;

        _showSummary = !_showSummary;
        await LoadTransactionOverviews();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.SaleReturn);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleReturnItemReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.SaleReturnItemReport);
    }

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
