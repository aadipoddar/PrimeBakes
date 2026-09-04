using PrimeBakes.Exports.Inventory.Summary;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class KitchenProductionItemMonthlyReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showAmount = false;
	private bool _showTransactionReturns = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;
	private DateTime _currentDateTime = DateTime.Now;

	private string _reportTitle = "Records";
	private readonly List<string> _monthFields = [.. Enumerable.Range(1, 12).Select(month => $"Month{month}")];
	private List<string> _monthHeaders = [.. Enumerable.Repeat(string.Empty, 12)];

	private ProductModel? _selectedProduct = null;
	private ProductCategoryModel? _selectedProductCategory = null;
	private CompanyModel? _selectedCompany = null;
	private KitchenModel? _selectedKitchen = null;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<KitchenProductionItemOverviewModel> _transactionOverviews = [];
	private List<KitchenProductionItemOverviewModel> _allTransactionOverviews = [];
	private List<KitchenProductionReturnItemOverviewModel> _allTransactionReturnOverviews = [];
	private List<KitchenProductionItemMonthlySummaryModel> _monthlySummaries = [];

	private SfGrid<KitchenProductionItemMonthlySummaryModel> _sfGrid;
	private CustomAutoComplete<ProductModel> _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Inventory, UserRoles.Reports], true);
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

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_currentDateTime = await CommonData.LoadCurrentDateTime();
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(DateRangeType.CurrentFinancialYear, _currentDateTime, _currentDateTime);

		var products = CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		var productCategories = CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		var companies = CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		var kitchens = CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen);

		_products = [.. (await products).OrderBy(s => s.Name)];
		_productCategories = [.. (await productCategories).OrderBy(s => s.Name)];
		_companies = [.. (await companies).OrderBy(s => s.Name)];
		_kitchens = [.. (await kitchens).OrderBy(s => s.Name)];
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

			var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
			var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

			var allTransactionOverviews = CommonData.LoadReportDataByDate<KitchenProductionItemOverviewModel>(InventoryNames.KitchenProductionItemOverview, fromDate, toDate);
			var allTransactionReturnOverviews = CommonData.LoadReportDataByDate<KitchenProductionReturnItemOverviewModel>(InventoryNames.KitchenProductionReturnItemOverview, fromDate, toDate);

			_allTransactionOverviews = await allTransactionOverviews;
			_allTransactionReturnOverviews = await allTransactionReturnOverviews;

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = InventoryRouteNames.KitchenProductionItemMonthlyReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFormFactor = platform.FormFactor,
				CreatedPlatform = platform.Platform,
				CreatedLatitude = platform.Latitude,
				CreatedLongitude = platform.Longitude
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
			_ = LocalDbService.SyncDataBackground();
		}
	}

	private async Task ApplyFilters()
	{
		_transactionOverviews = [.. _allTransactionOverviews];

		if (_showTransactionReturns)
			MergeTransactionAndReturns();

		_transactionOverviews = [.. _transactionOverviews.Where(t =>
				(_showDeleted || t.MasterStatus) &&
				(_selectedProduct is null || _selectedProduct.Id == 0 || t.ItemId == _selectedProduct.Id) &&
				(_selectedProductCategory is null || _selectedProductCategory.Id == 0 || t.ItemCategoryId == _selectedProductCategory.Id) &&
				(_selectedCompany is null || _selectedCompany.Id == 0 || t.CompanyId == _selectedCompany.Id) &&
				(_selectedKitchen is null || _selectedKitchen.Id == 0 || t.KitchenId == _selectedKitchen.Id))
			.OrderBy(t => t.TransactionDateTime)];

		BuildMonthlySummaries();

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void BuildMonthlySummaries()
	{
		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		_monthHeaders = KitchenProductionItemMonthlySummaryModel.BuildMonthHeaders(financialYearStart);
		_reportTitle = $"Records - {financialYearStart:MMM yyyy} to {financialYearStart.AddMonths(11):MMM yyyy} ({(_showAmount ? "Amount" : "Quantity")})";

		_monthlySummaries = [.. _transactionOverviews
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var productions = g.Where(t => t.Quantity > 0).ToList();

				var summary = new KitchenProductionItemMonthlySummaryModel
				{
					ItemId = g.Key,
					ItemName = g.First().ItemName,
					ItemCode = g.First().ItemCode,
					ItemCategoryId = g.First().ItemCategoryId,
					ItemCategoryName = g.First().ItemCategoryName,
					TotalQuantity = g.Sum(t => t.Quantity),
					TotalAmount = g.Sum(t => t.Total),
					ReturnQuantity = -g.Where(t => t.Quantity < 0).Sum(t => t.Quantity),
					ReturnAmount = -g.Where(t => t.Total < 0).Sum(t => t.Total),
					MinimumRate = productions.Count == 0 ? 0 : productions.Min(t => t.Rate),
					MaximumRate = productions.Count == 0 ? 0 : productions.Max(t => t.Rate),
					LastRate = productions.Count == 0 ? 0 : productions.OrderBy(t => t.TransactionDateTime).Last().Rate,
					TransactionCount = g.Select(t => t.MasterId).Distinct().Count(),
					KitchenCount = g.Select(t => t.KitchenId).Distinct().Count(),
					FirstProductionDateTime = g.Min(t => t.TransactionDateTime),
					LastProductionDateTime = g.Max(t => t.TransactionDateTime)
				};

				foreach (var transaction in g)
				{
					var monthIndex = ((transaction.TransactionDateTime.Year - financialYearStart.Year) * 12) + transaction.TransactionDateTime.Month - financialYearStart.Month;
					if (monthIndex is < 0 or > 11)
						continue;

					summary[monthIndex] += _showAmount ? transaction.Total : transaction.Quantity;
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
			summary.MonthsSinceLastProduction = summary.LastProductionDateTime is null
				? 0
				: ((_currentDateTime.Year - summary.LastProductionDateTime.Value.Year) * 12) + _currentDateTime.Month - summary.LastProductionDateTime.Value.Month;
		}
	}

	private static int FindMonthIndex(KitchenProductionItemMonthlySummaryModel summary, decimal value)
	{
		for (var index = 0; index < 12; index++)
			if (summary[index] == value)
				return index;

		return 0;
	}

	private void MergeTransactionAndReturns() =>
		_transactionOverviews.AddRange(_allTransactionReturnOverviews.Select(kpr => new KitchenProductionItemOverviewModel
		{
			Id = kpr.Id,
			ItemId = kpr.ItemId,
			ItemName = kpr.ItemName,
			ItemCode = kpr.ItemCode,
			ItemCategoryId = kpr.ItemCategoryId,
			ItemCategoryName = kpr.ItemCategoryName,

			Quantity = -kpr.Quantity,
			Rate = kpr.Rate,
			Total = -kpr.Total,

			ItemRemarks = kpr.ItemRemarks,

			MasterId = kpr.MasterId,
			TransactionNo = kpr.TransactionNo,
			CompanyId = kpr.CompanyId,
			CompanyName = kpr.CompanyName,

			TransactionDateTime = kpr.TransactionDateTime,
			FinancialYearId = kpr.FinancialYearId,
			FinancialYear = kpr.FinancialYear,

			KitchenId = kpr.KitchenId,
			KitchenName = kpr.KitchenName,
			KitchenProductionRemarks = kpr.KitchenProductionReturnRemarks,

			TotalItems = kpr.TotalItems,
			TotalQuantity = -kpr.TotalQuantity,
			TotalAmount = -kpr.TotalAmount,

			CreatedBy = kpr.CreatedBy,
			CreatedByName = kpr.CreatedByName,
			CreatedAt = kpr.CreatedAt,
			CreatedFormFactor = kpr.CreatedFormFactor,
			CreatedPlatform = kpr.CreatedPlatform,
			CreatedLatitude = kpr.CreatedLatitude,
			CreatedLongitude = kpr.CreatedLongitude,
			LastModifiedBy = kpr.LastModifiedBy,
			LastModifiedByUserName = kpr.LastModifiedByUserName,
			LastModifiedAt = kpr.LastModifiedAt,
			LastModifiedFormFactor = kpr.LastModifiedFormFactor,
			LastModifiedPlatform = kpr.LastModifiedPlatform,
			LastModifiedLatitude = kpr.LastModifiedLatitude,
			LastModifiedLongitude = kpr.LastModifiedLongitude,

			MasterStatus = kpr.MasterStatus
		}));
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

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await ApplyFilters();
	}

	private async Task OnKitchenChanged(KitchenModel value)
	{
		_selectedKitchen = value;
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

			var (stream, fileName) = KitchenProductionItemMonthlyReportExport.ExportReport(
				_monthlySummaries,
				_monthHeaders,
				_currentDateTime,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showAmount,
				_selectedProduct?.Id > 0 ? _selectedProduct : null,
				_selectedProductCategory?.Id > 0 ? _selectedProductCategory : null,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
				_selectedKitchen?.Id > 0 ? _selectedKitchen : null
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
	private async Task ToggleMeasure()
	{
		_showAmount = !_showAmount;
		await ApplyFilters();
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}

	private async Task ToggleTransactionReturns()
	{
		_showTransactionReturns = !_showTransactionReturns;
		await ApplyFilters();
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
