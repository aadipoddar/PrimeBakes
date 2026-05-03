using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Store.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Order;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Order;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Order.Reports;

public partial class OrderReport : IAsyncDisposable
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
	private string _selectedOrderStatus = "All";

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<OrderOverviewModel> _transactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "View Sale", Id = "ViewSale", IconCss = "e-icons e-link", Target = ".e-content" },
		new() { Text = "Export Sale PDF", Id = "ExportSalePDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Sale Excel", Id = "ExportSaleExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" },
	];

	private SfGrid<OrderOverviewModel> _sfGrid;

	private ToastNotification _toastNotification;
	private string _deleteTransactionNo = string.Empty;
	private int _deleteTransactionId = 0;
	private string _recoverTransactionNo = string.Empty;
	private int _recoverTransactionId = 0;

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

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_transactionOverviews = await CommonData.LoadTableDataByDate<OrderOverviewModel>(
				ViewNames.OrderOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (!_showDeleted)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

			if (_selectedLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			// Filter by order status
			if (_selectedOrderStatus == "Pending")
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.SaleId == null)];
			else if (_selectedOrderStatus == "Completed")
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.SaleId != null)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.LocationName)
					.Select(g => new OrderOverviewModel
					{
						LocationName = g.Key,
						TotalItems = g.Sum(t => t.TotalItems),
						TotalQuantity = g.Sum(t => t.TotalQuantity)
					})];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load orders: {ex.Message}", ToastType.Error);
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

	private async Task OnOrderStatusChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
	{
		_selectedOrderStatus = args.Value;
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
			var (stream, fileName) = await OrderReportExport.ExportReport(
					_transactionOverviews,
					ReportExportType.Excel,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_showSummary,
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
			var (stream, fileName) = await OrderReportExport.ExportReport(
					_transactionOverviews,
					ReportExportType.PDF,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_showSummary,
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

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, true, false, CodeType.Order);
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
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false, CodeType.Order);
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

	private async Task DownloadSelectedSalePdfInvoice()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrEmpty(_sfGrid.SelectedRecords.First().SaleTransactionNo))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().SaleTransactionNo, true, false, CodeType.Sale);
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

	private async Task DownloadSelectedSaleExcelInvoice()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrEmpty(_sfGrid.SelectedRecords.First().SaleTransactionNo))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().SaleTransactionNo, false, true, CodeType.Sale);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<OrderOverviewModel> args)
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

			case "ViewSale":
				await ViewSelectedSaleTransaction();
				break;

			case "ExportSalePDF":
				await DownloadSelectedSalePdfInvoice();
				break;

			case "ExportSaleExcel":
				await DownloadSelectedSaleExcelInvoice();
				break;
		}
	}

	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false, CodeType.Order);

		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", decodedTransactionNo.PageRouteName, "_blank");
		else
			NavigationManager.NavigateTo(decodedTransactionNo.PageRouteName);
	}

	private async Task ViewSelectedSaleTransaction()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrEmpty(_sfGrid.SelectedRecords.First().SaleTransactionNo))
			return;

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().SaleTransactionNo, false, false, CodeType.Sale);

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

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			if (_deleteTransactionId == 0)
			{
				await _toastNotification.ShowAsync("Error", "No transaction selected to delete.", ToastType.Error);
				return;
			}

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
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _deleteTransactionId);
		if (order is null)
		{
			await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
			return;
		}

		order.Status = false;
		order.LastModifiedBy = _user.Id;
		order.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		order.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await OrderData.DeleteTransaction(order);
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

			await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

			if (_recoverTransactionId == 0)
			{
				await _toastNotification.ShowAsync("Error", "No transaction selected to recover.", ToastType.Error);
				return;
			}

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
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _recoverTransactionId);
		if (order is null)
		{
			await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
			return;
		}

		order.Status = true;
		order.LastModifiedBy = _user.Id;
		order.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		order.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await OrderData.RecoverTransaction(order);
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
			case "ViewSelectedSale":
				await ViewSelectedSaleTransaction();
				break;
			case "DownloadSelectedSalePdf":
				await DownloadSelectedSalePdfInvoice();
				break;
			case "DownloadSelectedSaleExcel":
				await DownloadSelectedSaleExcelInvoice();
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

	private async Task ToggleDeleted()
	{
		if (_user.LocationId > 1)
			return;

		_showDeleted = !_showDeleted;
		await LoadTransactionOverviews();
		StateHasChanged();
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
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Order, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Order);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrderItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportOrderItem);
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
