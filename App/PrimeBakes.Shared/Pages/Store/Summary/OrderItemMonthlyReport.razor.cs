using PrimeBakes.Exports.Store.Summary;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Summary;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class OrderItemMonthlyReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;
	private DateTime _currentDateTime = DateTime.Now;

	private string _reportTitle = "Records";
	private readonly List<string> _monthFields = [.. Enumerable.Range(1, 12).Select(month => $"Month{month}")];
	private List<string> _monthHeaders = [.. Enumerable.Repeat(string.Empty, 12)];

	private ProductModel? _selectedProduct = null;
	private ProductCategoryModel? _selectedProductCategory = null;
	private LocationModel? _selectedLocation = null;
	private CompanyModel? _selectedCompany = null;

	private int _completedFilter = YesNoFilterOptions.All;
	private YesNoFilterOption _selectedCompleted;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<LocationModel> _pendingLocations = [];
	private List<CompanyModel> _companies = [];
	private List<OrderItemOverviewModel> _transactionOverviews = [];
	private List<OrderItemOverviewModel> _allTransactionOverviews = [];
	private List<OrderItemMonthlySummaryModel> _monthlySummaries = [];

	private SfGrid<OrderItemMonthlySummaryModel> _sfGrid;
	private CustomAutoComplete<ProductModel> _firstFocus;
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
		_currentDateTime = await CommonData.LoadCurrentDateTime();
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(DateRangeType.CurrentFinancialYear, _currentDateTime, _currentDateTime);

		var products = CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		var productCategories = CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		var locations = CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		var companies = CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);

		_products = [.. (await products).OrderBy(s => s.Name)];
		_productCategories = [.. (await productCategories).OrderBy(s => s.Name)];
		_locations = [.. (await locations).OrderBy(s => s.Name)];
		_companies = [.. (await companies).OrderBy(s => s.Name)];

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

			_currentDateTime = await CommonData.LoadCurrentDateTime();

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<OrderItemOverviewModel>(
				StoreNames.OrderItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = StoreRouteNames.OrderItemMonthlyReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFromPlatform = await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService)
			});

			await ApplyFilters();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
	}

	private async Task ApplyFilters()
	{
		_transactionOverviews = [.. _allTransactionOverviews.Where(t =>
				(_showDeleted || t.MasterStatus) &&
				(_selectedProduct is null || _selectedProduct.Id == 0 || t.ItemId == _selectedProduct.Id) &&
				(_selectedProductCategory is null || _selectedProductCategory.Id == 0 || t.ItemCategoryId == _selectedProductCategory.Id) &&
				(_selectedLocation is null || _selectedLocation.Id == 0 || t.LocationId == _selectedLocation.Id) &&
				(_selectedCompany is null || _selectedCompany.Id == 0 || t.CompanyId == _selectedCompany.Id) &&
				(_completedFilter == YesNoFilterOptions.All ||
					(_completedFilter == YesNoFilterOptions.Yes && t.SaleId is not null) ||
					(_completedFilter == YesNoFilterOptions.No && t.SaleId is null)))
			.OrderBy(t => t.TransactionDateTime)];

		_pendingLocations = [.. _locations.Where(l => !_transactionOverviews.Any(t => t.LocationId == l.Id))];

		BuildMonthlySummaries();

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void BuildMonthlySummaries()
	{
		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		_monthHeaders = OrderItemMonthlySummaryModel.BuildMonthHeaders(financialYearStart);
		_reportTitle = $"Records - {financialYearStart:MMM yyyy} to {financialYearStart.AddMonths(11):MMM yyyy}";

		_monthlySummaries = [.. _transactionOverviews
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var summary = new OrderItemMonthlySummaryModel
				{
					ItemId = g.Key,
					ItemName = g.First().ItemName,
					ItemCode = g.First().ItemCode,
					ItemCategoryId = g.First().ItemCategoryId,
					ItemCategoryName = g.First().ItemCategoryName,
					FulfilledQuantity = g.Where(t => t.SaleId is not null).Sum(t => t.Quantity),
					PendingQuantity = g.Where(t => t.SaleId is null).Sum(t => t.Quantity),
					OrderCount = g.Select(t => t.MasterId).Distinct().Count(),
					FulfilledOrderCount = g.Where(t => t.SaleId is not null).Select(t => t.MasterId).Distinct().Count(),
					LocationCount = g.Select(t => t.LocationId).Distinct().Count(),
					FirstOrderDateTime = g.Min(t => t.TransactionDateTime),
					LastOrderDateTime = g.Max(t => t.TransactionDateTime)
				};

				foreach (var transaction in g)
				{
					var monthIndex = ((transaction.TransactionDateTime.Year - financialYearStart.Year) * 12) + transaction.TransactionDateTime.Month - financialYearStart.Month;
					if (monthIndex is < 0 or > 11)
						continue;

					summary[monthIndex] += transaction.Quantity;
				}

				return summary;
			})
			.OrderByDescending(summary => summary.Total)
			.ThenBy(summary => summary.ItemName)];

		var grandTotal = _monthlySummaries.Sum(summary => summary.Total);

		for (var index = 0; index < _monthlySummaries.Count; index++)
		{
			var summary = _monthlySummaries[index];
			summary.Rank = index + 1;
			summary.ContributionPercent = grandTotal == 0 ? 0 : Math.Round(summary.Total / grandTotal * 100, 2);
			summary.PeakMonthName = summary.ActiveMonths == 0 ? string.Empty : _monthHeaders[FindMonthIndex(summary, summary.PeakMonthValue)];
			summary.LowestMonthName = summary.ActiveMonths == 0 ? string.Empty : _monthHeaders[FindMonthIndex(summary, summary.LowestMonthValue)];
			summary.MonthsSinceLastOrder = summary.LastOrderDateTime is null
				? 0
				: ((_currentDateTime.Year - summary.LastOrderDateTime.Value.Year) * 12) + _currentDateTime.Month - summary.LastOrderDateTime.Value.Month;
		}
	}

	private static int FindMonthIndex(OrderItemMonthlySummaryModel summary, decimal value)
	{
		for (var index = 0; index < 12; index++)
			if (summary[index] == value)
				return index;

		return 0;
	}
	#endregion

	#region Changed Events
	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadTransactionOverviews();
	}

	private async Task OnProductChanged(ProductModel value)
	{
		_selectedProduct = value;
		await ApplyFilters();
	}

	private async Task OnProductCategoryChanged(ProductCategoryModel value)
	{
		_selectedProductCategory = value;
		await ApplyFilters();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		_selectedLocation = value;
		await ApplyFilters();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await ApplyFilters();
	}

	private async Task OnCompletedChanged(YesNoFilterOption value)
	{
		_selectedCompleted = value;
		_completedFilter = value?.Id ?? YesNoFilterOptions.All;
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

			var (stream, fileName) = OrderItemMonthlyReportExport.ExportReport(
				_monthlySummaries,
				_monthHeaders,
				_currentDateTime,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_selectedProduct?.Id > 0 ? _selectedProduct : null,
				_selectedProductCategory?.Id > 0 ? _selectedProductCategory : null,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedLocation?.Id > 0 ? _selectedLocation : null
			);
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

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await ApplyFilters();
	}

	private async Task StartAutoRefresh()
	{
		var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 30;

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
