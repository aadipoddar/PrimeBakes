using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.StockTransfer;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.StockTransfer.Reports;

public partial class StockTransferReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
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

    private LocationModel _selectedLocation = new();
    private LocationModel _selectedToLocation = new();
    private CompanyModel _selectedCompany = new();

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<StockTransferOverviewModel> _transactionOverviews = [];

    private SfGrid<StockTransferOverviewModel> _sfGrid;

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

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Sales, UserRoles.Reports], true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
            .Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);

        await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadTransactionOverviews();
        await StartAutoRefresh();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
    }

    private async Task LoadLocations()
    {
        _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        _locations.Add(new()
        {
            Id = 0,
            Name = "All Locations"
        });
        _locations = [.. _locations.OrderBy(s => s.Name)];
        _selectedLocation = _locations.FirstOrDefault(_ => _.Id == _user.LocationId);
        _selectedToLocation = _locations.FirstOrDefault(_ => _.Id == 0);
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

    private async Task LoadTransactionOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

            _transactionOverviews = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(
                ViewNames.StockTransferOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (!_showDeleted)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

            if (_selectedLocation?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedToLocation?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.ToLocationId == _selectedToLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.ToLocationName)
                    .Select(g => new StockTransferOverviewModel
                    {
                        ToLocationName = g.Key,
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
        _selectedLocation = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnToLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        _selectedToLocation = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
    {
        _fromDate = dates.FromDate;
        _toDate = dates.ToDate;
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var (stream, fileName) = await StockTransferReportExport.ExportReport(
                    _transactionOverviews,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _showSummary,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null,
                    _selectedLocation?.Id > 0 ? _selectedLocation : null,
                    _selectedToLocation?.Id > 0 ? _selectedToLocation : null
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var (stream, fileName) = await StockTransferReportExport.ExportReport(
                    _transactionOverviews,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _showSummary,
                    _selectedCompany?.Id > 0 ? _selectedCompany : null,
                    _selectedLocation?.Id > 0 ? _selectedLocation : null,
                    _selectedToLocation?.Id > 0 ? _selectedToLocation : null
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
    #endregion

    #region Actions
    private async Task ViewSelectedCartItem()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await ViewTransaction(selectedCartItem.Id);
    }

    private async Task ViewTransaction(int transactionId)
    {
        try
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{transactionId}", "_blank");
            else
                NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{transactionId}");
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
        }
    }

    private async Task DownloadSelectedCartItemPdfInvoice()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await DownloadPdfInvoice(selectedCartItem.Id);
    }

    private async Task DownloadSelectedCartItemExcelInvoice()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await DownloadExcelInvoice(selectedCartItem.Id);
    }

    private async Task DownloadPdfInvoice(int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            var (pdfStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);

            await _toastNotification.ShowAsync("Success", "PDF invoice generated successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"PDF invoice generation failed: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DownloadExcelInvoice(int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            var (excelStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
            await SaveAndViewService.SaveAndView(fileName, excelStream);

            await _toastNotification.ShowAsync("Success", "Excel invoice generated successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Excel invoice generation failed: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DeleteSelectedCartItem()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();

        if (!selectedCartItem.Status)
            await ShowRecoverConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
        else
            await ShowDeleteConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
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

            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

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
        var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, _deleteTransactionId);
        if (stockTransfer is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to false (soft delete)
        stockTransfer.Status = false;
        stockTransfer.LastModifiedBy = _user.Id;
        stockTransfer.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        stockTransfer.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await StockTransferData.DeleteTransaction(stockTransfer);
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

            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

            if (_recoverTransactionId == 0)
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to recover.", ToastType.Error);
                return;
            }

            await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

            await RecoverTransaction();

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

    private async Task RecoverTransaction()
    {
        var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, _recoverTransactionId);
        if (stockTransfer is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to true (active)
        stockTransfer.Status = true;
        stockTransfer.LastModifiedBy = _user.Id;
        stockTransfer.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        stockTransfer.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await StockTransferData.RecoverTransaction(stockTransfer);
    }
    #endregion

    #region Utilities
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
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.StockTransfer, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.StockTransfer);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.StockTransferItemReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.StockTransferItemReport);
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.StoreDashboard);

    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

    private async Task ShowDeleteConfirmation(int id, string transactionNo)
    {
        _deleteTransactionId = id;
        _deleteTransactionNo = transactionNo;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteTransactionId = 0;
        _deleteTransactionNo = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ShowRecoverConfirmation(int id, string transactionNo)
    {
        _recoverTransactionId = id;
        _recoverTransactionNo = transactionNo;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
        await _recoverConfirmationDialog.HideAsync();
    }

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