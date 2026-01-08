using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Exporting.Sales.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Stock;

public partial class RawMaterialStockReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDetails = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now;
    private DateTime _toDate = DateTime.Now;

    private List<RawMaterialStockSummaryModel> _stockSummary = [];
    private List<RawMaterialStockDetailsModel> _stockDetails = [];

    private SfGrid<RawMaterialStockSummaryModel> _sfStockGrid;
    private SfGrid<RawMaterialStockDetailsModel> _sfStockDetailsGrid;

    private int _deleteAdjustmentId = 0;
    private string _deleteTransactionNo = string.Empty;
    private DeleteConfirmationDialog _deleteConfirmationDialog;

    private ToastNotification _toastNotification;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.R, LoadStockData, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadStockData, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
            .Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
            .Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
            .Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);


        await LoadDates();
        await LoadStockData();
        await StartAutoRefresh();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
    }

    private async Task LoadStockData()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Loading", "Fetching stock data...", ToastType.Info);

            _stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_fromDate, _toDate);

            _stockSummary = [.. _stockSummary.Where(_ => _.OpeningStock != 0 ||
                                                  _.PurchaseStock != 0 ||
                                                  _.SaleStock != 0 ||
                                                  _.ClosingStock != 0)];
            _stockSummary = [.. _stockSummary.OrderBy(_ => _.RawMaterialName)];

            if (_showDetails)
                await LoadStockDetails();
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to load stock: {ex.Message}", ToastType.Error);
        }
        finally
        {
            if (_sfStockGrid is not null)
                await _sfStockGrid.Refresh();

            if (_sfStockDetailsGrid is not null)
                await _sfStockDetailsGrid.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task LoadStockDetails()
    {
        _stockDetails = await CommonData.LoadTableDataByDate<RawMaterialStockDetailsModel>(
                ViewNames.RawMaterialStockDetails,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

        _stockDetails = [.. _stockDetails.OrderBy(_ => _.TransactionDateTime).ThenBy(_ => _.RawMaterialName)];
    }
    #endregion

    #region Changed Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadStockData();
    }

    private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
    {
        _fromDate = dates.FromDate;
        _toDate = dates.ToDate;
        await LoadStockData();
    }
    #endregion

    #region Exporting
    private async Task ExportExcel()
    {
        if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel file...", ToastType.Info);

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var (summaryStream, summaryFileName) = await RawMaterialStockReportExport.ExportSummaryReport(
                    _stockSummary,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns
                );

            await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);

            if (_showDetails && _stockDetails is not null && _stockDetails.Count > 0)
            {
                var (detailsStream, detailsFileName) = await RawMaterialStockReportExport.ExportDetailsReport(
                        _stockDetails,
                        ReportExportType.Excel,
                        dateRangeStart,
                        dateRangeEnd
                    );

                await SaveAndViewService.SaveAndView(detailsFileName, detailsStream);
            }

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
        if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF file...", ToastType.Info);

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var (summaryStream, summaryFileName) = await RawMaterialStockReportExport.ExportSummaryReport(
                    _stockSummary,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns
                );

            await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);

            if (_showDetails && _stockDetails is not null && _stockDetails.Count > 0)
            {
                var (detailsStream, detailsFileName) = await RawMaterialStockReportExport.ExportDetailsReport(
                        _stockDetails,
                        ReportExportType.PDF,
                        dateRangeStart,
                        dateRangeEnd
                    );

                await SaveAndViewService.SaveAndView(detailsFileName, detailsStream);
            }

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
        if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
        await ViewTransaction(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
    }

    private async Task ViewTransaction(string type, int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            var url = type?.ToLower() switch
            {
                "purchase" => $"{PageRouteNames.Purchase}/{transactionId}",
                "purchasereturn" => $"{PageRouteNames.PurchaseReturn}/{transactionId}",
                "sale" => $"{PageRouteNames.Sale}/{transactionId}",
                "salereturn" => $"{PageRouteNames.SaleReturn}/{transactionId}",
                "kitchenissue" => $"{PageRouteNames.KitchenIssue}/{transactionId}",
                "kitchenproduction" => $"{PageRouteNames.KitchenProduction}/{transactionId}",
                "stocktransfer" => $"{PageRouteNames.StockTransfer}/{transactionId}",
                _ => null
            };

            if (string.IsNullOrEmpty(url))
            {
                await _toastNotification.ShowAsync("Error", "Unknown transaction type.", ToastType.Error);
                return;
            }

            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", url, "_blank");
            else
                NavigationManager.NavigateTo(url);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
        }
    }

    private async Task DownloadSelectedCartItemPdfInvoice()
    {
        if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
        await DownloadPdfInvoice(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
    }

    private async Task DownloadSelectedCartItemExcelInvoice()
    {
        if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
        await DownloadExcelInvoice(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
    }

    private async Task DownloadPdfInvoice(string type, int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            if (type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await PurchaseInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("purchasereturn", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await PurchaseReturnInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("sale", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await SaleInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("salereturn", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await SaleReturnInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("kitchenissue", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await KitchenIssueInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("kitchenproduction", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await KitchenProductionInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (type.Equals("stocktransfer", StringComparison.CurrentCultureIgnoreCase))
            {
                var (pdfStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }

            await _toastNotification.ShowAsync("Success", "PDF invoice downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while downloading PDF invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DownloadExcelInvoice(string type, int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            if (type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await PurchaseInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("purchasereturn", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await PurchaseReturnInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("sale", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await SaleInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("salereturn", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await SaleReturnInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("kitchenissue", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await KitchenIssueInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("kitchenproduction", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await KitchenProductionInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (type.Equals("stocktransfer", StringComparison.CurrentCultureIgnoreCase))
            {
                var (excelStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }

            await _toastNotification.ShowAsync("Success", "Excel invoice downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while downloading Excel invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DeleteSelectedCartItem()
    {
        if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();

        if (selectedCartItem.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase))
            await ShowDeleteConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
    }

    private async Task ConfirmDelete()
    {
        if (_isProcessing || _deleteAdjustmentId == 0)
            return;

        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();
            StateHasChanged();

            if (_deleteAdjustmentId == 0)
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to delete.", ToastType.Error);
                return;
            }

            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

            await DeleteAdjustment();

            _deleteAdjustmentId = 0;
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
            await LoadStockData();
        }
    }

    private async Task DeleteAdjustment()
    {
        await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

        var adjustment = _stockDetails.FirstOrDefault(x => x.Id == _deleteAdjustmentId);
        if (adjustment is null || !adjustment.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase))
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found or is not an adjustment.", ToastType.Error);
            return;
        }

        await RawMaterialStockData.DeleteRawMaterialStockById(_deleteAdjustmentId, _user.Id);
        await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);
    }
    #endregion

    #region Utilities
    private async Task ToggleDetailsView()
    {
        _showDetails = !_showDetails;
        await LoadStockData();
    }

    private void ToggleColumnsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.RawMaterialStockAdjustment, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.RawMaterialStockAdjustment);
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

    private async Task ShowDeleteConfirmation(int id, string transactionNo)
    {
        _deleteAdjustmentId = id;
        _deleteTransactionNo = transactionNo ?? "N/A";
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteAdjustmentId = 0;
        _deleteTransactionNo = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task Logout() => await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

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
                await LoadStockData();
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