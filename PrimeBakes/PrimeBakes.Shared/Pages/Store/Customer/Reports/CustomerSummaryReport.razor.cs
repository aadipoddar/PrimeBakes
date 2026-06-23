using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Restaurant.Bill.Models;
using PrimeBakes.Library.Store.Customer.Exports;
using PrimeBakes.Library.Store.Customer.Models;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Customer.Reports;

public partial class CustomerSummaryReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;
	private DateTime _referenceDate = DateTime.Now.Date;

	private CompanyModel? _selectedCompany = null;
	private LocationModel? _selectedLocation = null;

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<CustomerModel> _customers = [];

	private List<SaleOverviewModel> _allSales = [];
	private List<SaleReturnOverviewModel> _allReturns = [];
	private List<BillOverviewModel> _allBills = [];

	private List<CustomerSummaryModel> _customerSummaries = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Refresh Data (Ctrl + R / F5)", Id = "Refresh", IconCss = "e-icons e-refresh", Target = ".e-content" },
		new() { Text = "Export PDF (Ctrl + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Ctrl + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<CustomerSummaryModel> _sfGrid;
	private CustomDateRangePicker _firstFocus;
	private string? activeBreakpoint { get; set; }

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store, UserRoles.Reports], true);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadTransactionOverviews();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_referenceDate = await CommonData.LoadCurrentDateTime();
		_fromDate = _referenceDate;
		_toDate = _referenceDate;

		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_customers = await CommonData.LoadTableData<CustomerModel>(StoreNames.Customer);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_locations = [.. _locations.OrderBy(s => s.Name)];

		_selectedLocation = _user.LocationId != 1 ? _locations.FirstOrDefault(s => s.Id == _user.LocationId) : null;
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

			var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
			var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

			_allSales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate);
			_allReturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate);
			_allBills = await CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate);

			await ApplyFilters();
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
			await _toastNotification.HideAllInfoAsync();
		}
	}

	private async Task ApplyFilters()
	{
		var sales = _allSales.Where(s => s.Status
			&& (_selectedCompany is null || _selectedCompany.Id == 0 || s.CompanyId == _selectedCompany.Id)
			&& (_selectedLocation is null || _selectedLocation.Id == 0 || s.LocationId == _selectedLocation.Id)).ToList();

		var bills = _allBills.Where(b => b.Status
			&& (_selectedCompany is null || _selectedCompany.Id == 0 || b.CompanyId == _selectedCompany.Id)
			&& (_selectedLocation is null || _selectedLocation.Id == 0 || b.LocationId == _selectedLocation.Id)).ToList();

		var returns = _allReturns.Where(r => r.Status
			&& (_selectedCompany is null || _selectedCompany.Id == 0 || r.CompanyId == _selectedCompany.Id)
			&& (_selectedLocation is null || _selectedLocation.Id == 0 || r.LocationId == _selectedLocation.Id)).ToList();

		CalculateTotals(sales, bills, returns);

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void CalculateTotals(List<SaleOverviewModel> sales, List<BillOverviewModel> bills, List<SaleReturnOverviewModel> returns)
	{
		_customerSummaries = [];

		foreach (var customer in _customers)
		{
			var customerSales = sales.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerBills = bills.Where(_ => _.CustomerId == customer.Id).ToList();
			var customerReturns = returns.Where(_ => _.CustomerId == customer.Id).ToList();

			if (customerSales.Count == 0 && customerBills.Count == 0 && customerReturns.Count == 0)
				continue;

			var summary = new CustomerSummaryModel
			{
				CustomerId = customer.Id,
				Name = customer.Name,
				Number = customer.Number,

				SaleCount = customerSales.Count,
				BillCount = customerBills.Count,
				ReturnCount = customerReturns.Count,

				SaleAmount = customerSales.Sum(_ => _.TotalAmount),
				BillAmount = customerBills.Sum(_ => _.TotalAmount),
				ReturnAmount = customerReturns.Sum(_ => _.TotalAmount),

				TotalQuantity = customerSales.Sum(_ => _.TotalQuantity) + customerBills.Sum(_ => _.TotalQuantity),

				Cash = customerSales.Sum(_ => _.Cash) + customerBills.Sum(_ => _.Cash),
				Card = customerSales.Sum(_ => _.Card) + customerBills.Sum(_ => _.Card),
				UPI = customerSales.Sum(_ => _.UPI) + customerBills.Sum(_ => _.UPI),
				Credit = customerSales.Sum(_ => _.Credit) + customerBills.Sum(_ => _.Credit),
			};

			var purchaseDates = customerSales.Select(_ => _.TransactionDateTime).Concat(customerBills.Select(_ => _.TransactionDateTime)).ToList();
			if (purchaseDates.Count > 0)
			{
				summary.FirstPurchase = purchaseDates.Min();
				summary.LastPurchase = purchaseDates.Max();
				summary.DaysSinceLastVisit = Math.Max(0, (_referenceDate.Date - summary.LastPurchase.Value.Date).Days);
			}

			_customerSummaries.Add(summary);
		}

		var totalNetBusiness = _customerSummaries.Sum(_ => _.NetBusiness);
		foreach (var summary in _customerSummaries)
			summary.ContributionPercent = totalNetBusiness == 0 ? 0 : Math.Round(summary.NetBusiness / totalNetBusiness * 100, 2);

		_customerSummaries = [.. _customerSummaries.OrderByDescending(_ => _.NetBusiness)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await LoadTransactionOverviews();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadTransactionOverviews();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await ApplyFilters();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		_selectedLocation = value;
		await ApplyFilters();
	}
	#endregion

	#region Exporting
	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await CustomerSummaryReportExport.ExportReport(
				_customerSummaries,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
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

	#region Utilities
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<CustomerSummaryModel> args)
	{
		switch (args.Item.Id)
		{
			case "Refresh": await LoadTransactionOverviews(); break;
			case "ExportPDF": await ExportReport(); break;
			case "ExportExcel": await ExportReport(true); break;
		}

	}
	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}

	// Weighted grand-totals for the percentage / average columns (a plain sum of
	// per-customer percentages would be meaningless), used by the custom footer aggregates.
	private decimal TotalReturnPercent
	{
		get
		{
			var gross = _customerSummaries.Sum(_ => _.GrossBusiness);
			return gross == 0 ? 0 : Math.Round(_customerSummaries.Sum(_ => _.ReturnAmount) / gross * 100, 2);
		}
	}

	private decimal TotalAverageOrderValue
	{
		get
		{
			var count = _customerSummaries.Sum(_ => _.PurchaseCount);
			return count == 0 ? 0 : Math.Round(_customerSummaries.Sum(_ => _.NetBusiness) / count, 2);
		}
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
