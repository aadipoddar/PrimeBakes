using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase.Reports;

public partial class PurchaseReturnReport : IAsyncDisposable
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

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<PurchaseReturnOverviewModel> _transactionOverviews = [];

    private readonly List<ContextMenuItemModel> _gridContextMenuItems =
    [
        new() { Text = "View", Id = "view", Target = ".e-content" },
        new()
        {
            Text = "Download",
            Id = "download",
            Target = ".e-content",
            Items =
            [
                new() { Text = "PDF", Id = "download-pdf" },
                new() { Text = "Excel", Id = "download-excel" },
                new() { Text = "Original", Id = "download-original" }
            ]
        },
        new() { Text = "Delete / Recover", Id = "delete-recover", Target = ".e-content" }
    ];

    private SfGrid<PurchaseReturnOverviewModel> _sfGrid;

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

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
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
            .Add(ModCode.Ctrl, Code.O, ViewSelectedTransaction, "Open Selected Transaction", Exclude.None)
            .Add(ModCode.Alt, Code.P, DownloadSelectedPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, DownloadSelectedExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
            .Add(Code.Delete, DeleteRecoverSelectedTransaction, "Delete Selected Transaction", Exclude.None);

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

            _transactionOverviews = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(
                ViewNames.PurchaseReturnOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

            if (!_showDeleted)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showSummary)
                _transactionOverviews = [.. _transactionOverviews
                    .GroupBy(t => t.PartyName)
                    .Select(g => new PurchaseReturnOverviewModel
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
                        CashDiscountAmount = g.Sum(t => t.CashDiscountAmount),
                        OtherChargesAmount = g.Sum(t => t.OtherChargesAmount),
                        RoundOffAmount = g.Sum(t => t.RoundOffAmount),
                        TotalAmount = g.Sum(t => t.TotalAmount)
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
            var (stream, fileName) = await PurchaseReturnReportExport.ExportReport(
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
            var (stream, fileName) = await PurchaseReturnReportExport.ExportReport(
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

    private async Task DownloadSelectedOriginalInvoice()
    {
        if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        try
        {
            var documentUrl = _sfGrid.SelectedRecords.First().DocumentUrl;

            if (string.IsNullOrEmpty(documentUrl))
            {
                await _toastNotification.ShowAsync("Warning", "No original document available for this purchase return.", ToastType.Warning);
                return;
            }

            _isProcessing = true;

            var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(documentUrl, BlobStorageContainers.purchasereturn);
            var fileName = documentUrl.Split('/').Last();
            await SaveAndViewService.SaveAndView(fileName, fileStream);

            await _toastNotification.ShowAsync("Success", "Invoice downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while downloading original invoice: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Actions
    private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<PurchaseReturnOverviewModel> args)
    {
        if (_showSummary)
            return;

        switch (args.Item.Id)
        {
            case "view":
                await ViewSelectedTransaction();
                break;

            case "download-pdf":
                await DownloadSelectedPdfInvoice();
                break;

            case "download-excel":
                await DownloadSelectedExcelInvoice();
                break;

            case "download-original":
                await DownloadSelectedOriginalInvoice();
                break;

            case "delete-recover":
                await DeleteRecoverSelectedTransaction();
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

    private async Task DeleteRecoverSelectedTransaction()
    {
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

            if (!_user.Admin)
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

            await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

            if (_deleteTransactionId == 0)
            {
                await _toastNotification.ShowAsync("Error", "Invalid transaction selected for deletion.", ToastType.Error);
                return;
            }

            await DeleteTransaction();

            await _toastNotification.ShowAsync("Success", $"Transaction '{_deleteTransactionNo}' has been successfully deleted.", ToastType.Success);

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
        var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, _deleteTransactionId);
        if (purchaseReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        purchaseReturn.Status = false;
        purchaseReturn.LastModifiedBy = _user.Id;
        purchaseReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        purchaseReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await PurchaseReturnData.DeleteTransaction(purchaseReturn);
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

            await _toastNotification.ShowAsync("Success", $"Transaction '{_recoverTransactionNo}' has been successfully recovered.", ToastType.Success);

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while recovering purchase transaction: {ex.Message}", ToastType.Error);
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
        var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, _recoverTransactionId);
        if (purchaseReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        purchaseReturn.Status = true;
        purchaseReturn.LastModifiedBy = _user.Id;
        purchaseReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        purchaseReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await PurchaseReturnData.RecoverTransaction(purchaseReturn);
    }
    #endregion

    #region Utilities
    private async Task ShowDeleteConfirmation()
    {
        _deleteTransactionId = _sfGrid.SelectedRecords.First().Id;
        _deleteTransactionNo = _sfGrid.SelectedRecords.First().ChallanNo;
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
        _recoverTransactionNo = _sfGrid.SelectedRecords.First().ChallanNo;
        StateHasChanged();
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
        await _recoverConfirmationDialog.HideAsync();
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
        _showDeleted = !_showDeleted;
        await LoadTransactionOverviews();
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

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.PurchaseReturnItemReport, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.PurchaseReturnItemReport);
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

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
