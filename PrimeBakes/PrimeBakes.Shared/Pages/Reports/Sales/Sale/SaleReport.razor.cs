using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Exporting.Sales.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReport : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private PeriodicTimer _autoRefreshTimer;
    private CancellationTokenSource _autoRefreshCts;

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showSummary = false;
    private bool _showSaleReturns = false;
    private bool _showStockTransfers = false;
    private bool _showDeleted = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel _selectedLocation = new();
    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<SaleOverviewModel> _transactionOverviews = [];
    private List<SaleReturnOverviewModel> _transactionReturnOverviews = [];
    private List<StockTransferOverviewModel> _transactionTransferOverviews = [];

    private SfGrid<SaleOverviewModel> _sfGrid;

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

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
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
            .Add(ModCode.Alt, Code.T, DownloadSelectedCartItemThermalInvoice, "Download Selected Transaction Thermal Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
            .Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
            .Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);

        await LoadDates();
        await LoadLocations();
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

            _transactionOverviews = await CommonData.LoadTableDataByDate<SaleOverviewModel>(
                ViewNames.SaleOverview,
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (!_showDeleted)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

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
                    .GroupBy(t => t.LocationName)
                    .Select(g => new SaleOverviewModel
                    {
                        LocationName = g.Key,
                        TotalAmount = g.Sum(t => t.TotalAmount),
                        TotalAfterItemDiscount = g.Sum(t => t.TotalAfterItemDiscount),
                        TotalAfterTax = g.Sum(t => t.TotalAfterTax),
                        BaseTotal = g.Sum(t => t.BaseTotal),
                        TotalInclusiveTaxAmount = g.Sum(t => t.TotalInclusiveTaxAmount),
                        TotalExtraTaxAmount = g.Sum(t => t.TotalExtraTaxAmount),
                        DiscountAmount = g.Sum(t => t.DiscountAmount),
                        ItemDiscountAmount = g.Sum(t => t.ItemDiscountAmount),
                        OtherChargesAmount = g.Sum(t => t.OtherChargesAmount),
                        RoundOffAmount = g.Sum(t => t.RoundOffAmount),
                        Card = g.Sum(t => t.Card),
                        Credit = g.Sum(t => t.Credit),
                        Cash = g.Sum(t => t.Cash),
                        UPI = g.Sum(t => t.UPI),
                        TotalQuantity = g.Sum(t => t.TotalQuantity),
                        TotalItems = g.Sum(t => t.TotalItems)
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

    private async Task LoadTransactionReturnOverviews()
    {
        _transactionReturnOverviews = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(
            ViewNames.SaleReturnOverview,
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

        if (!_showDeleted)
            _transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.Status)];

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
        _transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new SaleOverviewModel
        {
            Id = pr.Id * -1, // Negative ID to differentiate returns
            CompanyId = pr.CompanyId,
            CompanyName = pr.CompanyName,
            PartyId = pr.PartyId,
            PartyName = pr.PartyName,
            TransactionDateTime = pr.TransactionDateTime,
            OtherChargesAmount = -pr.OtherChargesAmount,
            RoundOffAmount = -pr.RoundOffAmount,
            TotalAmount = -pr.TotalAmount,
            TotalAfterItemDiscount = -pr.TotalAfterItemDiscount,
            TotalExtraTaxAmount = -pr.TotalExtraTaxAmount,
            TotalInclusiveTaxAmount = -pr.TotalInclusiveTaxAmount,
            BaseTotal = -pr.BaseTotal,
            CreatedAt = pr.CreatedAt,
            CreatedBy = pr.CreatedBy,
            CreatedByName = pr.CreatedByName,
            CreatedFromPlatform = pr.CreatedFromPlatform,
            DiscountAmount = -pr.DiscountAmount,
            DiscountPercent = pr.DiscountPercent,
            FinancialYear = pr.FinancialYear,
            FinancialYearId = pr.FinancialYearId,
            Remarks = pr.Remarks,
            LastModifiedAt = pr.LastModifiedAt,
            LastModifiedBy = pr.LastModifiedBy,
            LastModifiedByUserName = pr.LastModifiedByUserName,
            LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
            Card = -pr.Card,
            Credit = -pr.Credit,
            Cash = -pr.Cash,
            UPI = -pr.UPI,
            CustomerId = pr.CustomerId,
            CustomerName = pr.CustomerName,
            LocationId = pr.LocationId,
            LocationName = pr.LocationName,
            ItemDiscountAmount = -pr.ItemDiscountAmount,
            OrderDateTime = null,
            OrderId = null,
            OrderTransactionNo = null,
            TotalAfterTax = -pr.TotalAfterTax,
            TotalItems = pr.TotalItems,
            TotalQuantity = -pr.TotalQuantity,
            TransactionNo = pr.TransactionNo,
            PaymentModes = pr.PaymentModes,
            OtherChargesPercent = pr.OtherChargesPercent,
            Status = pr.Status
        }));

        _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
    }

    private async Task LoadTransactionTransferOverviews()
    {
        _transactionTransferOverviews = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(
            ViewNames.StockTransferOverview,
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

        if (!_showDeleted)
            _transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.Status)];

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
        _transactionOverviews.AddRange(_transactionTransferOverviews.Select(pr => new SaleOverviewModel
        {
            Id = 0, // Stock transfers do not have a sale ID
            CompanyId = pr.CompanyId,
            CompanyName = pr.CompanyName,
            PartyId = _locations.FirstOrDefault(l => l.LedgerId == pr.ToLocationId)?.Id,
            PartyName = _locations.FirstOrDefault(l => l.LedgerId == pr.ToLocationId)?.Name,
            TransactionDateTime = pr.TransactionDateTime,
            OtherChargesAmount = pr.OtherChargesAmount,
            RoundOffAmount = pr.RoundOffAmount,
            TotalAmount = pr.TotalAmount,
            TotalAfterItemDiscount = pr.TotalAfterItemDiscount,
            TotalExtraTaxAmount = pr.TotalExtraTaxAmount,
            TotalInclusiveTaxAmount = pr.TotalInclusiveTaxAmount,
            BaseTotal = pr.BaseTotal,
            CreatedAt = pr.CreatedAt,
            CreatedBy = pr.CreatedBy,
            CreatedByName = pr.CreatedByName,
            CreatedFromPlatform = pr.CreatedFromPlatform,
            DiscountAmount = pr.DiscountAmount,
            DiscountPercent = pr.DiscountPercent,
            FinancialYear = pr.FinancialYear,
            FinancialYearId = pr.FinancialYearId,
            Remarks = pr.Remarks,
            LastModifiedAt = pr.LastModifiedAt,
            LastModifiedBy = pr.LastModifiedBy,
            LastModifiedByUserName = pr.LastModifiedByUserName,
            LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
            Card = pr.Card,
            Credit = pr.Credit,
            Cash = pr.Cash,
            UPI = pr.UPI,
            LocationId = pr.LocationId,
            LocationName = pr.LocationName,
            ItemDiscountAmount = pr.ItemDiscountAmount,
            OrderDateTime = null,
            OrderId = null,
            OrderTransactionNo = null,
            CustomerId = null,
            CustomerName = null,
            TotalAfterTax = pr.TotalAfterTax,
            TotalItems = pr.TotalItems,
            TotalQuantity = pr.TotalQuantity,
            TransactionNo = pr.TransactionNo,
            PaymentModes = pr.PaymentModes,
            OtherChargesPercent = pr.OtherChargesPercent,
            Status = pr.Status
        }));

        _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
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

            var (stream, fileName) = await SaleReportExport.ExportReport(
                    _transactionOverviews,
                    ReportExportType.Excel,
                    dateRangeStart,
                    dateRangeEnd,
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var (stream, fileName) = await SaleReportExport.ExportReport(
                   _transactionOverviews,
                    ReportExportType.PDF,
                    dateRangeStart,
                    dateRangeEnd,
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
    #endregion

    #region Actions
    private async Task ViewSelectedCartItem()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await ViewTransaction(selectedCartItem.Id, selectedCartItem.TransactionNo);
    }

    private async Task ViewTransaction(int transactionId, string transactionNo)
    {
        try
        {
            if (transactionId == 0 && !string.IsNullOrEmpty(transactionNo))
            {
                var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{stockTransfer.Id}", "_blank");
                else
                    NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{stockTransfer.Id}");
            }
            else if (transactionId < 0)
            {
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{Math.Abs(transactionId)}", "_blank");
                else
                    NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{Math.Abs(transactionId)}");
            }
            else
            {
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{transactionId}", "_blank");
                else
                    NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{transactionId}");
            }
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to open transaction: {ex.Message}", ToastType.Error);
        }
    }

    private async Task DownloadSelectedCartItemThermalInvoice()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await DownloadThermalInvoice(selectedCartItem.Id, selectedCartItem.TransactionNo);
    }

    private async Task DownloadSelectedCartItemPdfInvoice()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await DownloadPdfInvoice(selectedCartItem.Id, selectedCartItem.TransactionNo);
    }

    private async Task DownloadSelectedCartItemExcelInvoice()
    {
        if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
            return;

        var selectedCartItem = _sfGrid.SelectedRecords.First();
        await DownloadExcelInvoice(selectedCartItem.Id, selectedCartItem.TransactionNo);
    }

    private async Task DownloadThermalInvoice(int transactionId, string transactionNo)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating thermal invoice...", ToastType.Info);

            if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
            {
                await _toastNotification.ShowAsync("Error", "Thermal invoices are not available for stock transfers.", ToastType.Error);
            }
            else if (transactionId < 0)
            {
                await _toastNotification.ShowAsync("Error", "Thermal invoices are not available for sale returns.", ToastType.Error);
            }
            else
            {
                var printStream = await SaleThermalPrint.GenerateThermalBill(transactionId);
                await JSRuntime.InvokeVoidAsync("printToPrinter", printStream.ToString());
            }

            await _toastNotification.ShowAsync("Success", "PDF invoice generated successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Thermal invoice generation failed: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DownloadPdfInvoice(int transactionId, string transactionNo)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

            if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
            {
                var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
                var (pdfStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(stockTransfer.Id, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else if (transactionId < 0)
            {
                var (pdfStream, fileName) = await SaleReturnInvoiceExport.ExportInvoice(Math.Abs(transactionId), InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else
            {
                var (pdfStream, fileName) = await SaleInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.PDF);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }

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

    private async Task DownloadExcelInvoice(int transactionId, string transactionNo)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

            if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
            {
                var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
                var (excelStream, fileName) = await StockTransferInvoiceExport.ExportInvoice(stockTransfer.Id, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else if (transactionId < 0)
            {
                var (excelStream, fileName) = await SaleReturnInvoiceExport.ExportInvoice(Math.Abs(transactionId), InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }
            else
            {
                var (excelStream, fileName) = await SaleInvoiceExport.ExportInvoice(transactionId, InvoiceExportType.Excel);
                await SaveAndViewService.SaveAndView(fileName, excelStream);
            }

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

            if (_deleteTransactionId == 0 && string.IsNullOrWhiteSpace(_deleteTransactionNo))
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to delete.", ToastType.Error);
                return;
            }

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

            await DeleteTransaction();

            _deleteTransactionId = 0;
            _deleteTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete transaction: {ex.Message}", ToastType.Error);
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
        await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

        if (_deleteTransactionId == 0 && !string.IsNullOrWhiteSpace(_deleteTransactionNo))
        {
            var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == _deleteTransactionNo);
            await DeleteStockTransferTransaction(stockTransfer.Id);
        }
        else if (_deleteTransactionId < 0)
            await DeleteSaleReturnTransaction(Math.Abs(_deleteTransactionId));
        else
            await DeleteSaleTransaction(_deleteTransactionId);

        await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);
    }

    private async Task DeleteSaleTransaction(int transactionId)
    {
        var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transactionId);
        if (sale is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to false (soft delete)
        sale.Status = false;
        sale.LastModifiedBy = _user.Id;
        sale.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        sale.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleData.DeleteTransaction(sale);
    }

    private async Task DeleteSaleReturnTransaction(int transactionId)
    {
        var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, transactionId);
        if (saleReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to false (soft delete)
        saleReturn.Status = false;
        saleReturn.LastModifiedBy = _user.Id;
        saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleReturnData.DeleteTransaction(saleReturn);
    }

    private async Task DeleteStockTransferTransaction(int transactionId)
    {
        var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, transactionId);
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

            if (_recoverTransactionId == 0 && string.IsNullOrWhiteSpace(_recoverTransactionNo))
            {
                await _toastNotification.ShowAsync("Error", "No transaction selected to recover.", ToastType.Error);
                return;
            }

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

            await RecoverTransaction();

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover transaction: {ex.Message}", ToastType.Error);
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
        await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

        if (_recoverTransactionId == 0 && !string.IsNullOrWhiteSpace(_recoverTransactionNo))
        {
            var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == _recoverTransactionNo);
            await RecoverStockTransferTransaction(stockTransfer.Id);
        }
        else if (_recoverTransactionId < 0)
            await RecoverSaleReturnTransaction(Math.Abs(_recoverTransactionId));
        else
            await RecoverSaleTransaction(_recoverTransactionId);

        await _toastNotification.ShowAsync("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", ToastType.Success);
    }

    private async Task RecoverSaleTransaction(int recoverTransactionId)
    {
        var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, recoverTransactionId);
        if (sale is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to true (active)
        sale.Status = true;
        sale.LastModifiedBy = _user.Id;
        sale.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        sale.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleData.RecoverTransaction(sale);
    }

    private async Task RecoverSaleReturnTransaction(int recoverTransactionId)
    {
        var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, recoverTransactionId);
        if (saleReturn is null)
        {
            await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
            return;
        }

        // Update the Status to true (active)
        saleReturn.Status = true;
        saleReturn.LastModifiedBy = _user.Id;
        saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
        saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

        await SaleReturnData.RecoverTransaction(saleReturn);
    }

    private async Task RecoverStockTransferTransaction(int recoverTransactionId)
    {
        var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, recoverTransactionId);
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
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Sale, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Sale);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem);
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private void NavigateBack() =>
        NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

    private async Task ShowDeleteConfirmation(int id, string transactionNo)
    {
        _deleteTransactionId = id;
        _deleteTransactionNo = transactionNo;
        StateHasChanged();
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
        StateHasChanged();
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