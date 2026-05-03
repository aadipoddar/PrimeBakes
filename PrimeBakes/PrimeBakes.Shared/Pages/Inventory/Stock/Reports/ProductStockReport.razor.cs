using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Store.StockTransfer;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Stock.Reports;

public partial class ProductStockReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDetails = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now;
	private DateTime _toDate = DateTime.Now;

	private LocationModel _selectedLocation;

	private List<LocationModel> _locations = [];
	private List<ProductStockSummaryModel> _stockSummary = [];
	private List<ProductStockDetailsModel> _stockDetails = [];
	private readonly List<ContextMenuItemModel> _detailsGridContextMenuItems =
	[
		new() { Text = "View (Ctrl + O)", Id = "ViewSelected", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "DownloadSelectedPdf", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "DownloadSelectedExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelected", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<ProductStockSummaryModel> _sfStockGrid;
	private SfGrid<ProductStockDetailsModel> _sfStockDetailsGrid;

	private int _deleteAdjustmentId = 0;
	private string _deleteTransactionNo = string.Empty;
	private DeleteConfirmationDialog _deleteConfirmationDialog;

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
		await LoadLocations();
		await LoadStockData();
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
		try
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations = [.. _locations.OrderBy(s => s.Name)];
			_selectedLocation = _locations.FirstOrDefault(_ => _.Id == 1);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Locations", ex.Message, ToastType.Error);
		}
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

			_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue),
				 _selectedLocation.Id);

			_stockSummary = [.. _stockSummary.Where(_ => _.OpeningStock != 0 ||
												  _.PurchaseStock != 0 ||
												  _.SaleStock != 0 ||
												  _.ClosingStock != 0)];
			_stockSummary = [.. _stockSummary.OrderBy(_ => _.ProductName)];

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
		_stockDetails = await CommonData.LoadTableDataByDate<ProductStockDetailsModel>(
				ViewNames.ProductStockDetails,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_stockDetails = [.. _stockDetails.Where(_ => _.LocationId == _selectedLocation.Id)];
		_stockDetails = [.. _stockDetails.OrderBy(_ => _.TransactionDateTime).ThenBy(_ => _.ProductName)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadStockData();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (args.Value is null || args.Value.Id <= 0)
			_selectedLocation = _locations.FirstOrDefault(l => l.Id == _user.LocationId);
		else
			_selectedLocation = args.Value;
		await LoadStockData();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
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

			if (_showDetails && (_stockDetails is null || _stockDetails.Count == 0))
				await LoadStockDetails();
			var (summaryStream, summaryFileName) = await ProductStockReportExport.ExportSummaryReport(
					_stockSummary,
					ReportExportType.Excel,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_selectedLocation?.Id > 0 ? _selectedLocation : null
				);

			await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);

			if (_showDetails && _stockDetails is not null && _stockDetails.Count > 0)
			{
				var (detailsStream, detailsFileName) = await ProductStockReportExport.ExportDetailsReport(
						_showDetails ? _stockDetails : null,
						ReportExportType.Excel,
						DateOnly.FromDateTime(_fromDate),
						DateOnly.FromDateTime(_toDate),
						_selectedLocation?.Id > 0 ? _selectedLocation : null
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
			var (summaryStream, summaryFileName) = await ProductStockReportExport.ExportSummaryReport(
					_stockSummary,
					ReportExportType.PDF,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_selectedLocation?.Id > 0 ? _selectedLocation : null
				);

			await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);

			if (_showDetails && _stockDetails is not null && _stockDetails.Count > 0)
			{
				var (detailsStream, detailsFileName) = await ProductStockReportExport.ExportDetailsReport(
						_showDetails ? _stockDetails : null,
						ReportExportType.PDF,
						DateOnly.FromDateTime(_fromDate),
						DateOnly.FromDateTime(_toDate),
						_selectedLocation?.Id > 0 ? _selectedLocation : null
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

		if (!selectedCartItem.TransactionId.HasValue)
		{
			await _toastNotification.ShowAsync("Transaction Not Available", "The selected row does not have an associated transaction.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(selectedCartItem.TransactionNo, false, false);
		await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task DownloadSelectedCartItemPdfInvoice()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();

		if (!selectedCartItem.TransactionId.HasValue)
		{
			await _toastNotification.ShowAsync("Invoice Not Available", "The selected row does not have an associated transaction invoice.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(selectedCartItem.TransactionNo, true, false);
		await SaveAndViewService.SaveAndView(decodedTransactionNo.PDFStream.fileName, decodedTransactionNo.PDFStream.stream);
	}

	private async Task DownloadSelectedCartItemExcelInvoice()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();

		if (!selectedCartItem.TransactionId.HasValue)
		{
			await _toastNotification.ShowAsync("Invoice Not Available", "The selected row does not have an associated transaction invoice.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(selectedCartItem.TransactionNo, false, true);
		await SaveAndViewService.SaveAndView(decodedTransactionNo.ExcelStream.fileName, decodedTransactionNo.ExcelStream.stream);
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

		await ProductStockData.DeleteProductStockById(_deleteAdjustmentId, _user.Id);
		await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);
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
				await LoadStockData();
				break;
			case "ToggleColumnsView":
				ToggleColumnsView();
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
				await ViewSelectedCartItem();
				break;
			case "DownloadSelectedPdf":
				await DownloadSelectedCartItemPdfInvoice();
				break;
			case "DownloadSelectedExcel":
				await DownloadSelectedCartItemExcelInvoice();
				break;
			case "DeleteSelected":
				await DeleteSelectedCartItem();
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

	private async Task OnDetailsGridContextMenuItemClicked(ContextMenuClickEventArgs<ProductStockDetailsModel> args)
	{
		switch (args.Item.Id)
		{
			case "ViewSelected":
				await ViewSelectedCartItem();
				break;
			case "DownloadSelectedPdf":
				await DownloadSelectedCartItemPdfInvoice();
				break;
			case "DownloadSelectedExcel":
				await DownloadSelectedCartItemExcelInvoice();
				break;
			case "DeleteSelected":
				await DeleteSelectedCartItem();
				break;
		}
	}

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

	private async Task NavigateToTransactionPage() =>
		await AuthenticationService.NavigateToRoute(PageRouteNames.ProductStockAdjustment, FormFactor, JSRuntime, NavigationManager);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

	private async Task ShowDeleteConfirmation(int id, string transactionNo)
	{
		_deleteAdjustmentId = id;
		_deleteTransactionNo = transactionNo ?? "N/A";
		StateHasChanged();
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteAdjustmentId = 0;
		_deleteTransactionNo = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
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
				await LoadStockData();
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
