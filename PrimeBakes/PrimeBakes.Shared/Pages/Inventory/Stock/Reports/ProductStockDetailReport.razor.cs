using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Exports;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Stock.Reports;

public partial class ProductStockDetailReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation = null;

	private List<LocationModel> _locations = [];
	private List<ProductStockDetailsModel> _stockDetails = [];
	private readonly List<ContextMenuItemModel> _detailsGridContextMenuItems =
	[
		new() { Text = "View (Ctrl + O)", Id = "ViewSelected", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "DownloadSelectedPdf", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "DownloadSelectedExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteSelected", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<ProductStockDetailsModel> _sfStockDetailsGrid;
	private CustomDateRangePicker _sfFirstFocus;

	private int _deleteAdjustmentId = 0;
	private string _deleteTransactionNo = string.Empty;
	private DeleteConfirmationDialog _deleteConfirmationDialog;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadStockDetails();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;

		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_locations = [.. _locations.OrderBy(l => l.Name)];
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == 1) ?? _locations.FirstOrDefault();
	}

	private async Task LoadStockDetails()
	{
		if (_isProcessing || _selectedLocation is null)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching stock details...", ToastType.Info, 0);

			_stockDetails = await CommonData.LoadTableDataByDate<ProductStockDetailsModel>(
				InventoryNames.ProductStockDetails,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			_stockDetails = [.. _stockDetails
				.Where(d => d.LocationId == _selectedLocation.Id)
				.OrderBy(d => d.TransactionDateTime).ThenBy(d => d.ProductName)];

			if (_sfStockDetailsGrid is not null)
				await _sfStockDetailsGrid.Refresh();
			await _toastNotification.HideAllAsync();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load stock details: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await LoadStockDetails();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadStockDetails();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		_selectedLocation = value ?? _locations.FirstOrDefault(l => l.Id == 1) ?? _locations.FirstOrDefault();
		await LoadStockDetails();
	}
	#endregion

	#region Exporting
	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing || _stockDetails is null || _stockDetails.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info, 0);

			var (stream, fileName) = await ProductStockReportExport.ExportDetailsReport(
				_stockDetails,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_selectedLocation?.Id > 0 ? _selectedLocation : null);
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
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
			await LoadStockDetails();
		}
	}

	private async Task DeleteAdjustment()
	{
		await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info, 0);

		var adjustment = _stockDetails.FirstOrDefault(x => x.Id == _deleteAdjustmentId);
		if (adjustment is null || !adjustment.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase))
		{
			await _toastNotification.ShowAsync("Error", "Transaction not found or is not an adjustment.", ToastType.Error);
			return;
		}

		await ProductStockData.DeleteProductStockById(_deleteAdjustmentId, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());
		await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);
	}
	#endregion

	#region Utilities
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
				await LoadStockDetails();
		}
		catch (OperationCanceledException) { }
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
