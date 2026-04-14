using PrimeBakes.Shared.Components.Dialog;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Store.Sale;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Store.Sale;
using PrimeBakesLibrary.Models.Store.StockTransfer;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class OutletSummaryReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();

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

	private SfGrid<OutletSummaryModel> _sfGrid;
	private string? activeBreakpoint { get; set; }

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Reports]);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.R, RefreshReport, "Refresh Data", Exclude.None)
			.Add(Code.F5, RefreshReport, "Refresh Data", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None);

		await LoadDates();
		await LoadCompanies();
		await RefreshReport();
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
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

		_purchases = await CommonData.LoadTableDataByDate<PurchaseOverviewModel>(
			ViewNames.PurchaseOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_purchasesReturns = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(
			ViewNames.PurchaseReturnOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_kitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(
			ViewNames.KitchenIssueOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_kitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(
			ViewNames.KitchenProductionOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_sales = await CommonData.LoadTableDataByDate<SaleOverviewModel>(
			ViewNames.SaleOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_salereturns = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(
			ViewNames.SaleReturnOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_stockTransfers = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(
			ViewNames.StockTransferOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

		_bills = await CommonData.LoadTableDataByDate<BillOverviewModel>(
			ViewNames.BillOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

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
				var ledgerLocation = await LedgerData.LoadLedgerByLocationId(outlet.LocationId);

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

			_outletSummaries.Add(outlet);
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await RefreshReport();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
		await RefreshReport();
	}

	private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
	{
		_fromDate = dates.FromDate;
		_toDate = dates.ToDate;
		await RefreshReport();
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

			var (stream, fileName) = await OutletSummaryReportExport.ExportReport(
				_outletSummaries,
				ReportExportType.Excel,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_selectedCompany?.Id > 0 ? _selectedCompany : null);

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

			var (stream, fileName) = await OutletSummaryReportExport.ExportReport(
				_outletSummaries,
				ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_selectedCompany?.Id > 0 ? _selectedCompany : null);

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

	#region Utilities
	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

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
				await RefreshReport();
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