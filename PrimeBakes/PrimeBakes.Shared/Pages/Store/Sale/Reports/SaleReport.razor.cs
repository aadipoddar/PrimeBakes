using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Sale;
using PrimeBakesLibrary.Data.Store.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Store.Sale;
using PrimeBakesLibrary.Models.Store.StockTransfer;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showBills = false;
	private bool _showSaleReturns = false;
	private bool _showStockTransfers = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel? _selectedLocation = null;
	private CompanyModel? _selectedCompany = null;
	private LedgerModel? _selectedParty = null;

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleOverviewModel> _transactionOverviews = [];
	private List<SaleReturnOverviewModel> _transactionReturnOverviews = [];
	private List<StockTransferOverviewModel> _transactionTransferOverviews = [];
	private List<BillOverviewModel> _transactionBillOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export Thermal (Alt + T)", Id = "ExportThermal", IconCss = "e-icons e-print", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

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

			_transactionOverviews = await CommonData.LoadTableDataByDate<SaleOverviewModel>(
				ViewNames.SaleOverview,
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

			if (_showSaleReturns)
				await LoadTransactionReturnOverviews();

			if (_showStockTransfers)
				await LoadTransactionTransferOverviews();

			if (_showBills)
				await LoadTransactionBillOverviews();

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
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

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
			Id = pr.Id,
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
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

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

		_transactionTransferOverviews = [.. _transactionTransferOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndTransfers();
	}

	private void MergeTransactionAndTransfers()
	{
		_transactionOverviews.AddRange(_transactionTransferOverviews.Select(pr => new SaleOverviewModel
		{
			Id = pr.Id,
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

	private async Task LoadTransactionBillOverviews()
	{
		_transactionBillOverviews = await CommonData.LoadTableDataByDate<BillOverviewModel>(
			ViewNames.BillOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_transactionBillOverviews = [.. _transactionBillOverviews.Where(_ => _.Status)];

		if (_selectedLocation?.Id > 0)
			_transactionBillOverviews = [.. _transactionBillOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

		if (_selectedCompany?.Id > 0)
			_transactionBillOverviews = [.. _transactionBillOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		_transactionBillOverviews = [.. _transactionBillOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndBills();
	}

	private void MergeTransactionAndBills()
	{
		_transactionOverviews.AddRange(_transactionBillOverviews.Select(pr => new SaleOverviewModel
		{
			Id = pr.Id,
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = null,
			PartyName = null,
			TransactionDateTime = pr.TransactionDateTime,
			OtherChargesAmount = pr.ServiceChargeAmount,
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
			OtherChargesPercent = pr.ServiceChargePercent,
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
			var (stream, fileName) = await SaleReportExport.ExportReport(
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
			var (stream, fileName) = await SaleReportExport.ExportReport(
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

	private async Task DownloadSelectedThermalInvoice()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating thermal invoice...", ToastType.Info);

			await ThermalPrintDispatcher.PrintAsync(
				() => SaleThermalPrint.GenerateThermalBill(_sfGrid.SelectedRecords.First().Id),
				() => SaleThermalPrint.GenerateThermalBillPng(_sfGrid.SelectedRecords.First().Id));

			await _toastNotification.ShowAsync("Success", "Thermal invoice generated successfully.", ToastType.Success);
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
	#endregion

	#region Actions
	private async Task PostDaywiseSales()
	{
		if (_isProcessing)
			return;

		try
		{
			if (_user.LocationId != 1 || _selectedLocation is null || _selectedLocation.Id <= 1)
			{
				await _toastNotification.ShowAsync("Validation", "Please select a specific location before day closing.", ToastType.Warning);
				return;
			}

			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Closing day and posting sale accounting...", ToastType.Info);

			for (var date = _fromDate; date <= _toDate; date = date.AddDays(1))
				await SaleData.PostDaySales(
					date,
					_selectedLocation.Id,
					_user.Id,
					FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Success", "Day closing completed successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Day closing failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<SaleOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View":
				await ViewSelectedTransaction();
				break;

			case "ExportThermal":
				await DownloadSelectedThermalInvoice();
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

		try
		{
			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false);

			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", decodedTransactionNo.PageRouteName, "_blank");
			else
				NavigationManager.NavigateTo(decodedTransactionNo.PageRouteName);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
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

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_deleteTransactionNo, false, false);

		if (decodedTransactionNo.CodeType == CodeType.StockTransfer)
			await DeleteStockTransferTransaction(_deleteTransactionId);
		else if (decodedTransactionNo.CodeType == CodeType.SaleReturn)
			await DeleteSaleReturnTransaction(_deleteTransactionId);
		else if (decodedTransactionNo.CodeType == CodeType.Sale)
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
			case "ToggleBills":
				await ToggleBills();
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
			case "PostDaywiseSales":
				await PostDaywiseSales();
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
			case "DownloadSelectedThermal":
				await DownloadSelectedThermalInvoice();
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
		_deleteTransactionId = _sfGrid.SelectedRecords.First().Id;
		_deleteTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
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
		_recoverTransactionNo = _sfGrid.SelectedRecords.First().TransactionNo;
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

	private async Task ToggleSaleReturns()
	{
		_showSaleReturns = !_showSaleReturns;
		await LoadTransactionOverviews();
	}

	private async Task ToggleBills()
	{
		_showBills = !_showBills;
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
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleItemReport, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.SaleItemReport);
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
