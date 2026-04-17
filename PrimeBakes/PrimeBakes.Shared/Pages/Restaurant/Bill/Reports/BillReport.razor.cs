using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.Data.Restaurant.Bill;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Store.Masters;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Reports;

public partial class BillReport : IAsyncDisposable
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
	private CompanyModel _selectedCompany = new();
	private CustomerModel _selectedCustomer = new();

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<CustomerModel> _customers = [];
	private List<BillOverviewModel> _transactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Ctrl + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export Thermal (Alt + T)", Id = "ExportThermal", IconCss = "e-icons e-print", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<BillOverviewModel> _sfGrid;

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

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant, UserRoles.Reports]);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		LoadHotKeys();

		await LoadDates();
		await LoadLocations();
		await LoadCompanies();
		await LoadCustomers();
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

	private async Task LoadCustomers()
	{
		_customers = await CommonData.LoadTableData<CustomerModel>(TableNames.Customer);
		_customers.Add(new()
		{
			Id = 0,
			Name = "All Customers"
		});
		_customers = [.. _customers.OrderBy(s => s.Name)];
		_selectedCustomer = _customers.FirstOrDefault(_ => _.Id == 0);
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

			_transactionOverviews = await CommonData.LoadTableDataByDate<BillOverviewModel>(
				ViewNames.BillOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (!_showDeleted)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

			if (_selectedLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedCustomer?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CustomerId == _selectedCustomer.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.LocationName)
					.Select(g => new BillOverviewModel
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
						ServiceChargeAmount = g.Sum(t => t.ServiceChargeAmount),
						RoundOffAmount = g.Sum(t => t.RoundOffAmount),
						Card = g.Sum(t => t.Card),
						Credit = g.Sum(t => t.Credit),
						Cash = g.Sum(t => t.Cash),
						UPI = g.Sum(t => t.UPI),
						TotalQuantity = g.Sum(t => t.TotalQuantity),
						TotalItems = g.Sum(t => t.TotalItems),
						TotalPeople = g.Sum(t => t.TotalPeople)
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

	private async Task OnCustomerChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CustomerModel, CustomerModel> args)
	{
		if (_user.LocationId > 1)
			return;

		_selectedCustomer = args.Value;
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
			var (stream, fileName) = await BillReportExport.ExportReport(
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
			var (stream, fileName) = await BillReportExport.ExportReport(
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

			var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

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
				() => BillThermalPrint.GenerateThermalBill(_sfGrid.SelectedRecords.First().Id),
				() => BillThermalPrint.GenerateThermalBillPng(_sfGrid.SelectedRecords.First().Id));

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
	private async Task PostDaywiseBills()
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
			await _toastNotification.ShowAsync("Processing", "Closing day and posting bill accounting...", ToastType.Info);

			for (var date = _fromDate; date <= _toDate; date = date.AddDays(1))
				await BillData.PostDayBills(
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

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<BillOverviewModel> args)
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

			if (_deleteTransactionId <= 0 && string.IsNullOrWhiteSpace(_deleteTransactionNo))
			{
				await _toastNotification.ShowAsync("Error", "No transaction selected to delete.", ToastType.Error);
				return;
			}

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, _deleteTransactionId);
			if (bill is null)
			{
				await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
				return;
			}

			bill.Status = false;
			bill.LastModifiedBy = _user.Id;
			bill.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			bill.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await BillData.DeleteTransaction(bill);

			await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);

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

	private async Task ConfirmRecover()
	{
		if (_isProcessing)
			return;

		try
		{
			await _recoverConfirmationDialog.HideAsync();
			_isProcessing = true;
			StateHasChanged();

			if (_recoverTransactionId <= 0 && string.IsNullOrWhiteSpace(_recoverTransactionNo))
			{
				await _toastNotification.ShowAsync("Error", "No transaction selected to recover.", ToastType.Error);
				return;
			}

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

			await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

			var bill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, _recoverTransactionId);
			if (bill is null)
			{
				await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
				return;
			}

			bill.Status = true;
			bill.LastModifiedBy = _user.Id;
			bill.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			bill.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await BillData.RecoverTransaction(bill);

			await _toastNotification.ShowAsync("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", ToastType.Success);

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
	#endregion

	#region Utilities
	private void LoadHotKeys()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(ModCode.Ctrl, Code.Delete, ToggleDeleted, "Show/Hide Deleted", Exclude.None)
			.Add(ModCode.Ctrl, Code.W, ToggleSummary, "Show/Hide Summary", Exclude.None)
			.Add(ModCode.Ctrl, Code.Q, ToggleDetailsView, "Toggle Details", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedTransaction, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.T, DownloadSelectedThermalInvoice, "Download Selected Transaction Thermal Invoice", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadSelectedExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
			.Add(Code.Delete, DeleteRecoverSelectedTransaction, "Delete / Recover Selected Transaction", Exclude.None);
	}

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
			case "PostDaywiseBills":
				await PostDaywiseBills();
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
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Bill, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Bill);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.BillItemReport, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.BillItemReport);
	}

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.RestaurantDashboard);

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

