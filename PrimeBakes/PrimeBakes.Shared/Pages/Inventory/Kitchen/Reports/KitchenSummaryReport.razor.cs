using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Inventory.Kitchen.Exports;
using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.Reports;

public partial class KitchenSummaryReport : IAsyncDisposable
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

	private List<KitchenSummaryModel> _kitchenSummaries = [];
	private List<KitchenModel> _kitchens = [];
	private List<KitchenIssueOverviewModel> _kitchenIssue = [];
	private List<KitchenProductionOverviewModel> _kitchenProduction = [];
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Refresh Data (Ctrl + R / F5)", Id = "Refresh", IconCss = "e-icons e-refresh", Target = ".e-content" },
		new() { Text = "Export PDF (Ctrl + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Ctrl + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<KitchenSummaryModel> _sfGrid;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
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

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
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
			await _toastNotification.HideAllInfoAsync();
		}
	}

	private async Task LoadTransactionOverviews()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen);

		var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
		var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

		_kitchenIssue = await CommonData.LoadTableDataByDate<KitchenIssueOverviewModel>(InventoryNames.KitchenIssueOverview, fromDate, toDate);
		_kitchenProduction = await CommonData.LoadTableDataByDate<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, fromDate, toDate);
	}

	private async Task ApplyFilters()
	{
		_kitchenIssue = [.. _kitchenIssue.Where(_ => _.Status)];
		_kitchenProduction = [.. _kitchenProduction.Where(_ => _.Status)];

		if (_selectedCompany?.Id > 0)
		{
			_kitchenIssue = [.. _kitchenIssue.Where(_ => _.CompanyId == _selectedCompany.Id)];
			_kitchenProduction = [.. _kitchenProduction.Where(_ => _.CompanyId == _selectedCompany.Id)];
		}
	}

	private async Task CalculateTotals()
	{
		_kitchenSummaries = [];

		foreach (var kitchen in _kitchens)
		{
			var outlet = new KitchenSummaryModel
			{
				KitchenId = kitchen.Id,
				KitchenName = kitchen.Name,
			};

			var kitchenIssues = _kitchenIssue.Where(_ => _.KitchenId == kitchen.Id).ToList();
			var kitchenProductions = _kitchenProduction.Where(_ => _.KitchenId == kitchen.Id).ToList();

			outlet.KitchenIssue = kitchenIssues.Sum(_ => _.TotalAmount);
			outlet.KitchenProduction = kitchenProductions.Sum(_ => _.TotalAmount);

			outlet.TransactionCount = kitchenIssues.Count + kitchenProductions.Count;
			outlet.UnitsProduced = kitchenProductions.Sum(_ => _.TotalQuantity);

			_kitchenSummaries.Add(outlet);
		}

		var totalNetProduction = _kitchenSummaries.Sum(_ => _.NetProduction);
		foreach (var outlet in _kitchenSummaries)
			outlet.ContributionPercent = totalNetProduction == 0 ? 0 : Math.Round(outlet.NetProduction / totalNetProduction * 100, 2);
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

			var (stream, fileName) = await KitchenSummaryReportExport.ExportReport(
				_kitchenSummaries,
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenSummaryModel> args)
	{
		switch (args.Item.Id)
		{
			case "Refresh": await RefreshReport(); break;
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