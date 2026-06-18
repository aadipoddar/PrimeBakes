using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class OutletSummaryReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel? _selectedCompany = null;

	private List<CompanyModel> _companies = [];

	private List<OutletSummaryModel> _outletSummaries = [];
	private List<LocationModel> _locations = [];
	private List<PurchaseOverviewModel> _purchases = [];
	private List<PurchaseReturnOverviewModel> _purchasesReturns = [];
	private List<KitchenIssueOverviewModel> _kitchenIssue = [];
	private List<KitchenProductionOverviewModel> _kitchenProduction = [];
	private List<SaleOverviewModel> _sales = [];
	private List<SaleReturnOverviewModel> _salereturns = [];
	private List<StockTransferOverviewModel> _stockTransfers = [];
	private List<BillOverviewModel> _bills = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Refresh Data (Ctrl + R / F5)", Id = "Refresh", IconCss = "e-icons e-refresh", Target = ".e-content" },
		new() { Text = "Export PDF (Ctrl + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Ctrl + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<OutletSummaryModel> _sfGrid;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Reports], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		await LoadDates();
		await LoadCompanies();
		await RefreshReport();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadDates()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;
	}

	private async Task LoadCompanies()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_companies = [.. _companies.OrderBy(s => s.Name)];
	}

	private async Task RefreshReport()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			await LoadTransactionOverviews();
			await ApplyFilters();
			await CalculateTotals();
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

	private async Task LoadTransactionOverviews()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);

		var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
		var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

		_purchases = await CommonData.LoadTableDataByDate<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, fromDate, toDate);
		_purchasesReturns = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(InventoryNames.PurchaseReturnOverview, fromDate, toDate);
		_kitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, fromDate, toDate);
		_kitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, fromDate, toDate);
		_sales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate);
		_salereturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate);
		_stockTransfers = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(StoreNames.StockTransferOverview, fromDate, toDate);
		_bills = await CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate);
	}

	private async Task ApplyFilters()
	{
		_purchases = [.. _purchases.Where(_ => _.Status)];
		_purchasesReturns = [.. _purchasesReturns.Where(_ => _.Status)];
		_kitchenIssue = [.. _kitchenIssue.Where(_ => _.Status)];
		_kitchenProduction = [.. _kitchenProduction.Where(_ => _.Status)];
		_sales = [.. _sales.Where(_ => _.Status)];
		_salereturns = [.. _salereturns.Where(_ => _.Status)];
		_stockTransfers = [.. _stockTransfers.Where(_ => _.Status)];
		_bills = [.. _bills.Where(_ => _.Status)];

		if (_selectedCompany?.Id > 0)
		{
			_purchases = [.. _purchases.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_purchasesReturns = [.. _purchasesReturns.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_kitchenIssue = [.. _kitchenIssue.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_kitchenProduction = [.. _kitchenProduction.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_sales = [.. _sales.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_salereturns = [.. _salereturns.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_stockTransfers = [.. _stockTransfers.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_bills = [.. _bills.Where(_ => _.CompanyId == _selectedCompany.Id)];
		}
	}

	private async Task CalculateTotals()
	{
		_outletSummaries = [];

		foreach (var location in _locations)
		{
			var outlet = new OutletSummaryModel()
			{
				LocationId = location.Id,
				LocationName = location.Name,
			};

			if (location.Id == 1)
			{
				outlet.Purchase = _purchases.Sum(_ => _.TotalAmount);
				outlet.PurchaseReturn = _purchasesReturns.Sum(_ => _.TotalAmount);
				outlet.KitchenIssue = _kitchenIssue.Sum(_ => _.TotalAmount);
				outlet.KitchenProduction = _kitchenProduction.Sum(_ => _.TotalAmount);
				outlet.Sale =
					_sales.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount) +
					_bills.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount);
				outlet.SaleReturn = _salereturns.Where(_ => _.LocationId == 1).Sum(_ => _.TotalAmount);
			}

			else
			{
				var ledgerLocation = await LocationData.LoadLedgerByLocationId(outlet.LocationId);

				outlet.Purchase =
					_sales.Where(_ => _.PartyId == ledgerLocation.Id).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.ToLocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
				outlet.PurchaseReturn = _salereturns.Where(_ => _.PartyId == ledgerLocation.Id).Sum(_ => _.TotalAmount);
				outlet.KitchenIssue = 0;
				outlet.KitchenProduction = 0;
				outlet.Sale =
					_sales.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount) +
					_stockTransfers.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount) +
					_bills.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
				outlet.SaleReturn = _salereturns.Where(_ => _.LocationId == outlet.LocationId).Sum(_ => _.TotalAmount);
			}

			var sales = _sales.Where(_ => _.LocationId == outlet.LocationId).ToList();
			var transfers = _stockTransfers.Where(_ => _.LocationId == outlet.LocationId).ToList();
			var bills = _bills.Where(_ => _.LocationId == outlet.LocationId).ToList();

			outlet.UnitsSold = sales.Sum(_ => _.TotalQuantity) + transfers.Sum(_ => _.TotalQuantity) + bills.Sum(_ => _.TotalQuantity);
			outlet.Cash = sales.Sum(_ => _.Cash) + transfers.Sum(_ => _.Cash) + bills.Sum(_ => _.Cash);
			outlet.Card = sales.Sum(_ => _.Card) + transfers.Sum(_ => _.Card) + bills.Sum(_ => _.Card);
			outlet.UPI = sales.Sum(_ => _.UPI) + transfers.Sum(_ => _.UPI) + bills.Sum(_ => _.UPI);
			outlet.Credit = sales.Sum(_ => _.Credit) + transfers.Sum(_ => _.Credit) + bills.Sum(_ => _.Credit);

			_outletSummaries.Add(outlet);
		}

		var totalNetSale = _outletSummaries.Sum(_ => _.NetSale);
		foreach (var outlet in _outletSummaries)
			outlet.ContributionPercent = totalNetSale == 0 ? 0 : Math.Round(outlet.NetSale / totalNetSale * 100, 2);
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await RefreshReport();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await RefreshReport();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await RefreshReport();
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

			var (stream, fileName) = await OutletSummaryReportExport.ExportReport(
				_outletSummaries,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_selectedCompany?.Id > 0 ? _selectedCompany : null);

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
	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}

	// Weighted grand-totals for the percentage / average columns (a plain sum of
	// per-outlet percentages would be meaningless), used by the custom footer aggregates.
	private decimal TotalMarginPercent
	{
		get
		{
			var netSale = _outletSummaries.Sum(_ => _.NetSale);
			return netSale == 0 ? 0 : Math.Round(_outletSummaries.Sum(_ => _.GrossProfit) / netSale * 100, 2);
		}
	}

	private decimal TotalSaleReturnPercent
	{
		get
		{
			var sale = _outletSummaries.Sum(_ => _.Sale);
			return sale == 0 ? 0 : Math.Round(_outletSummaries.Sum(_ => _.SaleReturn) / sale * 100, 2);
		}
	}

	private decimal TotalAverageSaleValue
	{
		get
		{
			var count = _outletSummaries.Sum(_ => _.TransactionCount);
			return count == 0 ? 0 : Math.Round(_outletSummaries.Sum(_ => _.NetSale) / count, 2);
		}
	}

	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<OutletSummaryModel> args)
	{
		switch (args.Item.Id)
		{
			case "Refresh":
				await RefreshReport();
				break;
			case "ExportPDF":
				await ExportReport();
				break;
			case "ExportExcel":
				await ExportReport(true);
				break;
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
				await RefreshReport();
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
