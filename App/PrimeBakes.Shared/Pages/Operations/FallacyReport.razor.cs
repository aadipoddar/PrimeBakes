using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Restaurant.Bill;
using PrimeBakes.Exports.Store.Sale;
using PrimeBakes.Exports.Store.StockTransfer;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class FallacyReport : IAsyncDisposable
{
	private const int MainLocationId = 1;

	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private int _activeTabIndex;
	private int _customerMaxDiscountThreshold;
	private decimal _maxRoundOffAmount;
	private int _oddTimingStartHour;
	private int _oddTimingEndHour;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;
	private DateTime _currentDateTime = DateTime.Now;

	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];
	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];
	private List<StockTransferOverviewModel> _stockTransferOverviews = [];
	private List<BillOverviewModel> _billOverviews = [];

	// One list per tab
	private List<SaleOverviewModel> _fallacyDiscountMainOutletSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyDiscountMainOutletSaleReturnOverviews = [];
	private List<StockTransferOverviewModel> _fallacyDiscountMainOutletStockTransferOverviews = [];
	private List<SaleOverviewModel> _fallacyDiscountOutletCustomerSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyDiscountOutletCustomerSaleReturnOverviews = [];
	private List<BillOverviewModel> _fallacyDiscountCustomerBillOverviews = [];

	// Credit on outlet customer sales
	private List<SaleOverviewModel> _fallacyOddTimingSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyOddTimingSaleReturnOverviews = [];
	private List<StockTransferOverviewModel> _fallacyOddTimingStockTransferOverviews = [];
	private List<BillOverviewModel> _fallacyOddTimingBillOverviews = [];
	private List<SaleOverviewModel> _fallacyCreditOutletCustomerSaleOverviews = [];
	private List<BillOverviewModel> _fallacyCreditCustomerBillOverviews = [];

	// Missing accounting posting
	private List<SaleOverviewModel> _fallacyMissingPostingSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyMissingPostingSaleReturnOverviews = [];
	private List<StockTransferOverviewModel> _fallacyMissingPostingStockTransferOverviews = [];
	private List<BillOverviewModel> _fallacyMissingPostingBillOverviews = [];

	// Large round-off
	private List<SaleOverviewModel> _fallacyRoundOffSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyRoundOffSaleReturnOverviews = [];
	private List<StockTransferOverviewModel> _fallacyRoundOffStockTransferOverviews = [];
	private List<BillOverviewModel> _fallacyRoundOffBillOverviews = [];

	// Possible duplicates
	private List<SaleOverviewModel> _fallacyDuplicateSaleOverviews = [];
	private List<SaleReturnOverviewModel> _fallacyDuplicateSaleReturnOverviews = [];
	private List<StockTransferOverviewModel> _fallacyDuplicateStockTransferOverviews = [];
	private List<BillOverviewModel> _fallacyDuplicateBillOverviews = [];

	// One grid per tab
	private SfGrid<SaleOverviewModel> _sfGridMainOutletSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridMainOutletSaleReturn;
	private SfGrid<StockTransferOverviewModel> _sfGridMainOutletStockTransfer;
	private SfGrid<SaleOverviewModel> _sfGridOutletCustomerSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridOutletCustomerSaleReturn;
	private SfGrid<BillOverviewModel> _sfGridCustomerBill;
	private SfGrid<SaleOverviewModel> _sfGridMissingPostingSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridMissingPostingSaleReturn;
	private SfGrid<StockTransferOverviewModel> _sfGridMissingPostingStockTransfer;
	private SfGrid<BillOverviewModel> _sfGridMissingPostingBill;
	private SfGrid<SaleOverviewModel> _sfGridRoundOffSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridRoundOffSaleReturn;
	private SfGrid<StockTransferOverviewModel> _sfGridRoundOffStockTransfer;
	private SfGrid<BillOverviewModel> _sfGridRoundOffBill;
	private SfGrid<SaleOverviewModel> _sfGridDuplicateSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridDuplicateSaleReturn;
	private SfGrid<StockTransferOverviewModel> _sfGridDuplicateStockTransfer;
	private SfGrid<BillOverviewModel> _sfGridDuplicateBill;
	private SfGrid<SaleOverviewModel> _sfGridOddTimingSale;
	private SfGrid<SaleReturnOverviewModel> _sfGridOddTimingSaleReturn;
	private SfGrid<StockTransferOverviewModel> _sfGridOddTimingStockTransfer;
	private SfGrid<BillOverviewModel> _sfGridOddTimingBill;
	private SfGrid<SaleOverviewModel> _sfGridCreditOutletCustomerSale;
	private SfGrid<BillOverviewModel> _sfGridCreditCustomerBill;

	private CustomDateRangePicker _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Admin], true, OperationRouteNames.FallacyReport);
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
		_fromDate = _toDate = _currentDateTime;

		var locations = CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location, useLocalDB: true);
		var discountSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.CustomerMaxDiscountThreshold);
		_customerMaxDiscountThreshold = int.TryParse(discountSetting?.Value, out var discount) ? discount : 50;

		var roundOffSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.MaxRoundOffAmount);
		_maxRoundOffAmount = decimal.TryParse(roundOffSetting?.Value, out var roundOff) ? roundOff : 1;

		var oddStartSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.OddTimingStartHour);
		_oddTimingStartHour = int.TryParse(oddStartSetting?.Value, out var oddStart) ? oddStart : 0;

		var oddEndSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.OddTimingEndHour);
		_oddTimingEndHour = int.TryParse(oddEndSetting?.Value, out var oddEnd) ? oddEnd : 5;

		_locations = [.. (await locations).OrderBy(s => s.Name)];
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_currentDateTime = await CommonData.LoadCurrentDateTime();

			var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
			var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

			var saleOverviews = CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate, useLocalDB: true);
			var saleReturnOverviews = CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate, useLocalDB: true);
			var stockTransferOverviews = CommonData.LoadTableDataByDate<StockTransferOverviewModel>(StoreNames.StockTransferOverview, fromDate, toDate, useLocalDB: true);
			var billOverviews = CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate, useLocalDB: true);

			_saleOverviews = await saleOverviews;
			_saleReturnOverviews = await saleReturnOverviews;
			_stockTransferOverviews = await stockTransferOverviews;
			_billOverviews = await billOverviews;

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = OperationRouteNames.FallacyReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFormFactor = platform.FormFactor,
				CreatedPlatform = platform.Platform,
				CreatedLatitude = platform.Latitude,
				CreatedLongitude = platform.Longitude
			});

			ApplyFilters();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
		}
		finally
		{
			await RefreshGrids();
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
			_ = LocalDbService.SyncDataBackground();
		}
	}

	private void ApplyFilters()
	{
		BuildDiscountConflict();
		BuildMissingPostingConflict();
		BuildRoundOffConflict();
		BuildDuplicateConflict();
		BuildOddTimingConflict();
		BuildCreditConflict();
	}

	// Outlet (not main) whose ledger is the party of the transaction
	private LocationModel GetOutletByLedger(int? partyId) =>
		partyId is null ? null : _locations.FirstOrDefault(l => l.Id != MainLocationId && l.LedgerId == partyId);

	// Main outlet to outlet: flag if the discount is not equal to the outlet's discount
	// Outlet to customer: flag if the discount is greater than the customer max discount threshold
	private void BuildDiscountConflict()
	{
		_fallacyDiscountMainOutletSaleOverviews = [.. _saleOverviews
			.Where(t =>
				t.Status &&
				t.LocationId == MainLocationId &&
				GetOutletByLedger(t.PartyId) is { } outlet &&
				t.DiscountPercent != outlet.Discount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyDiscountMainOutletSaleReturnOverviews = [.. _saleReturnOverviews
			.Where(t =>
				t.Status &&
				t.LocationId == MainLocationId &&
				GetOutletByLedger(t.PartyId) is { } outlet &&
				t.DiscountPercent != outlet.Discount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyDiscountMainOutletStockTransferOverviews = [.. _stockTransferOverviews
			.Where(t =>
				t.Status &&
				t.LocationId == MainLocationId &&
				_locations.FirstOrDefault(l => l.Id == t.ToLocationId && l.Id != MainLocationId) is { } outlet &&
				t.DiscountPercent != outlet.Discount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyDiscountOutletCustomerSaleOverviews = [.. _saleOverviews
			.Where(t =>
				t.Status &&
				t.LocationId != MainLocationId &&
				t.DiscountPercent > _customerMaxDiscountThreshold)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyDiscountOutletCustomerSaleReturnOverviews = [.. _saleReturnOverviews
			.Where(t =>
				t.Status &&
				t.LocationId != MainLocationId &&
				t.DiscountPercent > _customerMaxDiscountThreshold)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyDiscountCustomerBillOverviews = [.. _billOverviews
			.Where(t =>
				t.Status &&
				t.DiscountPercent > _customerMaxDiscountThreshold)
			.OrderBy(t => t.TransactionDateTime)];
	}

	// Outlets sell for cash, UPI or card: flag any credit on outlet customer sales and bills
	private void BuildCreditConflict()
	{
		_fallacyCreditOutletCustomerSaleOverviews = [.. _saleOverviews
			.Where(t => t.Status && t.LocationId != MainLocationId && t.Credit > 0)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyCreditCustomerBillOverviews = [.. _billOverviews
			.Where(t => t.Status && t.Credit > 0)
			.OrderBy(t => t.TransactionDateTime)];
	}

	// Transactions with an amount, from earlier days, that have no accounting posting
	private void BuildMissingPostingConflict()
	{
		var today = _currentDateTime.Date;

		_fallacyMissingPostingSaleOverviews = [.. _saleOverviews
			.Where(t => t.Status && t.TotalAmount != 0 && t.TransactionDateTime.Date < today && string.IsNullOrWhiteSpace(t.FinancialAccountingTransactionNo))
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyMissingPostingSaleReturnOverviews = [.. _saleReturnOverviews
			.Where(t => t.Status && t.TotalAmount != 0 && t.TransactionDateTime.Date < today && string.IsNullOrWhiteSpace(t.FinancialAccountingTransactionNo))
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyMissingPostingStockTransferOverviews = [.. _stockTransferOverviews
			.Where(t => t.Status && t.TotalAmount != 0 && t.TransactionDateTime.Date < today && string.IsNullOrWhiteSpace(t.FinancialAccountingTransactionNo))
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyMissingPostingBillOverviews = [.. _billOverviews
			.Where(t => t.Status && t.TotalAmount != 0 && t.TransactionDateTime.Date < today && string.IsNullOrWhiteSpace(t.FinancialAccountingTransactionNo))
			.OrderBy(t => t.TransactionDateTime)];
	}

	// Round-off (either direction) above the allowed amount
	private void BuildRoundOffConflict()
	{
		_fallacyRoundOffSaleOverviews = [.. _saleOverviews
			.Where(t => t.Status && Math.Abs(t.RoundOffAmount) > _maxRoundOffAmount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyRoundOffSaleReturnOverviews = [.. _saleReturnOverviews
			.Where(t => t.Status && Math.Abs(t.RoundOffAmount) > _maxRoundOffAmount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyRoundOffStockTransferOverviews = [.. _stockTransferOverviews
			.Where(t => t.Status && Math.Abs(t.RoundOffAmount) > _maxRoundOffAmount)
			.OrderBy(t => t.TransactionDateTime)];

		_fallacyRoundOffBillOverviews = [.. _billOverviews
			.Where(t => t.Status && Math.Abs(t.RoundOffAmount) > _maxRoundOffAmount)
			.OrderBy(t => t.TransactionDateTime)];
	}

	// Same location, same ledger (party, where the transaction has one), same total, same minute
	private void BuildDuplicateConflict()
	{
		_fallacyDuplicateSaleOverviews = GetPossibleDuplicates(
			_saleOverviews.Where(t => t.Status),
			t => (t.LocationId, t.PartyId, t.TotalAmount, Minute: t.TransactionDateTime.Ticks / TimeSpan.TicksPerMinute),
			t => t.TransactionDateTime);

		_fallacyDuplicateSaleReturnOverviews = GetPossibleDuplicates(
			_saleReturnOverviews.Where(t => t.Status),
			t => (t.LocationId, t.PartyId, t.TotalAmount, Minute: t.TransactionDateTime.Ticks / TimeSpan.TicksPerMinute),
			t => t.TransactionDateTime);

		_fallacyDuplicateStockTransferOverviews = GetPossibleDuplicates(
			_stockTransferOverviews.Where(t => t.Status),
			t => (t.LocationId, t.ToLocationId, t.TotalAmount, Minute: t.TransactionDateTime.Ticks / TimeSpan.TicksPerMinute),
			t => t.TransactionDateTime);

		_fallacyDuplicateBillOverviews = GetPossibleDuplicates(
			_billOverviews.Where(t => t.Status),
			t => (t.LocationId, t.TotalAmount, Minute: t.TransactionDateTime.Ticks / TimeSpan.TicksPerMinute),
			t => t.TransactionDateTime);
	}

	// Entered (created) between the start hour and the end hour, for example 00:00 to 05:00
	private void BuildOddTimingConflict()
	{
		bool IsOdd(DateTime createdAt) => createdAt.Hour >= _oddTimingStartHour && createdAt.Hour < _oddTimingEndHour;

		_fallacyOddTimingSaleOverviews = [.. _saleOverviews
			.Where(t => t.Status && IsOdd(t.CreatedAt))
			.OrderBy(t => t.CreatedAt)];

		_fallacyOddTimingSaleReturnOverviews = [.. _saleReturnOverviews
			.Where(t => t.Status && IsOdd(t.CreatedAt))
			.OrderBy(t => t.CreatedAt)];

		_fallacyOddTimingStockTransferOverviews = [.. _stockTransferOverviews
			.Where(t => t.Status && IsOdd(t.CreatedAt))
			.OrderBy(t => t.CreatedAt)];

		_fallacyOddTimingBillOverviews = [.. _billOverviews
			.Where(t => t.Status && IsOdd(t.CreatedAt))
			.OrderBy(t => t.CreatedAt)];
	}

	private static List<T> GetPossibleDuplicates<T, TKey>(IEnumerable<T> transactions, Func<T, TKey> groupKey, Func<T, DateTime> orderKey) =>
		[.. transactions
			.GroupBy(groupKey)
			.Where(g => g.Count() > 1)
			.SelectMany(g => g)
			.OrderBy(orderKey)];

	private async Task RefreshGrids()
	{
		if (_sfGridMainOutletSale is not null) await _sfGridMainOutletSale.Refresh();
		if (_sfGridMainOutletSaleReturn is not null) await _sfGridMainOutletSaleReturn.Refresh();
		if (_sfGridMainOutletStockTransfer is not null) await _sfGridMainOutletStockTransfer.Refresh();
		if (_sfGridOutletCustomerSale is not null) await _sfGridOutletCustomerSale.Refresh();
		if (_sfGridOutletCustomerSaleReturn is not null) await _sfGridOutletCustomerSaleReturn.Refresh();
		if (_sfGridCustomerBill is not null) await _sfGridCustomerBill.Refresh();
		if (_sfGridMissingPostingSale is not null) await _sfGridMissingPostingSale.Refresh();
		if (_sfGridMissingPostingSaleReturn is not null) await _sfGridMissingPostingSaleReturn.Refresh();
		if (_sfGridMissingPostingStockTransfer is not null) await _sfGridMissingPostingStockTransfer.Refresh();
		if (_sfGridMissingPostingBill is not null) await _sfGridMissingPostingBill.Refresh();
		if (_sfGridRoundOffSale is not null) await _sfGridRoundOffSale.Refresh();
		if (_sfGridRoundOffSaleReturn is not null) await _sfGridRoundOffSaleReturn.Refresh();
		if (_sfGridRoundOffStockTransfer is not null) await _sfGridRoundOffStockTransfer.Refresh();
		if (_sfGridRoundOffBill is not null) await _sfGridRoundOffBill.Refresh();
		if (_sfGridDuplicateSale is not null) await _sfGridDuplicateSale.Refresh();
		if (_sfGridDuplicateSaleReturn is not null) await _sfGridDuplicateSaleReturn.Refresh();
		if (_sfGridDuplicateStockTransfer is not null) await _sfGridDuplicateStockTransfer.Refresh();
		if (_sfGridDuplicateBill is not null) await _sfGridDuplicateBill.Refresh();
		if (_sfGridOddTimingSale is not null) await _sfGridOddTimingSale.Refresh();
		if (_sfGridOddTimingSaleReturn is not null) await _sfGridOddTimingSaleReturn.Refresh();
		if (_sfGridOddTimingStockTransfer is not null) await _sfGridOddTimingStockTransfer.Refresh();
		if (_sfGridOddTimingBill is not null) await _sfGridOddTimingBill.Refresh();
		if (_sfGridCreditOutletCustomerSale is not null) await _sfGridCreditOutletCustomerSale.Refresh();
		if (_sfGridCreditCustomerBill is not null) await _sfGridCreditCustomerBill.Refresh();
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
	#endregion

	#region Exporting
	// Exports only the grid of the active tab
	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var exportType = isExcel ? ReportExportType.Excel : ReportExportType.PDF;
			var fromDate = DateOnly.FromDateTime(_fromDate);
			var toDate = DateOnly.FromDateTime(_toDate);

			// Tab order (must match the razor):
			// 0: Sale (Main Outlet)
			// 1: Sale Return (Main Outlet)
			// 2: Stock Transfer (Main Outlet)
			// 3: Sale (Customer)
			// 4: Sale Return (Customer)
			// 5: Bill (Customer)
			// 6: Posting - Sale
			// 7: Posting - Sale Return
			// 8: Posting - Stock Transfer
			// 9: Posting - Bill
			// 10: Round Off - Sale
			// 11: Round Off - Sale Return
			// 12: Round Off - Stock Transfer
			// 13: Round Off - Bill
			// 14: Duplicate - Sale
			// 15: Duplicate - Sale Return
			// 16: Duplicate - Stock Transfer
			// 17: Duplicate - Bill
			// 18: Odd Timing - Sale
			// 19: Odd Timing - Sale Return
			// 20: Odd Timing - Stock Transfer
			// 21: Odd Timing - Bill
			// 22: Credit - Sale
			// 23: Credit - Bill
			var (stream, fileName) = _activeTabIndex switch
			{
				0 => SaleReportExport.ExportReport(_fallacyDiscountMainOutletSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				1 => SaleReturnReportExport.ExportReport(_fallacyDiscountMainOutletSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				2 => StockTransferReportExport.ExportReport(_fallacyDiscountMainOutletStockTransferOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				3 => SaleReportExport.ExportReport(_fallacyDiscountOutletCustomerSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				4 => SaleReturnReportExport.ExportReport(_fallacyDiscountOutletCustomerSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				5 => BillReportExport.ExportReport(_fallacyDiscountCustomerBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				6 => SaleReportExport.ExportReport(_fallacyMissingPostingSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				7 => SaleReturnReportExport.ExportReport(_fallacyMissingPostingSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				8 => StockTransferReportExport.ExportReport(_fallacyMissingPostingStockTransferOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				9 => BillReportExport.ExportReport(_fallacyMissingPostingBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				10 => SaleReportExport.ExportReport(_fallacyRoundOffSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true),
				11 => SaleReturnReportExport.ExportReport(_fallacyRoundOffSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true),
				12 => StockTransferReportExport.ExportReport(_fallacyRoundOffStockTransferOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true),
				13 => BillReportExport.ExportReport(_fallacyRoundOffBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true),
				14 => SaleReportExport.ExportReport(_fallacyDuplicateSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				15 => SaleReturnReportExport.ExportReport(_fallacyDuplicateSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				16 => StockTransferReportExport.ExportReport(_fallacyDuplicateStockTransferOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				17 => BillReportExport.ExportReport(_fallacyDuplicateBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				18 => SaleReportExport.ExportReport(_fallacyOddTimingSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				19 => SaleReturnReportExport.ExportReport(_fallacyOddTimingSaleReturnOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				20 => StockTransferReportExport.ExportReport(_fallacyOddTimingStockTransferOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				21 => BillReportExport.ExportReport(_fallacyOddTimingBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: false),
				22 => SaleReportExport.ExportReport(_fallacyCreditOutletCustomerSaleOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true),
				_ => BillReportExport.ExportReport(_fallacyCreditCustomerBillOverviews, currentDateTime, exportType, fromDate, toDate, showAllColumns: true)
			};

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

	private async Task ViewTransaction(string transactionNo)
	{
		try
		{
			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(transactionNo, false, false);
			await WindowNavigation.NavigateToRoute(decodedTransactionNo.PageRouteName);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
	}

	private async Task ExportTransaction(string transactionNo, bool isExcel = false)
	{
		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(transactionNo, !isExcel, isExcel);
			await SaveAndViewService.SaveAndView(isExcel ? decodeTransactionNo.ExcelStream.fileName : decodeTransactionNo.PDFStream.fileName,
				isExcel ? decodeTransactionNo.ExcelStream.stream : decodeTransactionNo.PDFStream.stream);

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

	#region Context Menu
	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private Task OnSaleContextMenuItemClicked(ContextMenuClickEventArgs<SaleOverviewModel> args) =>
		HandleContextMenuItem(args.Item.Id, args.RowInfo?.RowData?.TransactionNo);

	private Task OnSaleReturnContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnOverviewModel> args) =>
		HandleContextMenuItem(args.Item.Id, args.RowInfo?.RowData?.TransactionNo);

	private Task OnStockTransferContextMenuItemClicked(ContextMenuClickEventArgs<StockTransferOverviewModel> args) =>
		HandleContextMenuItem(args.Item.Id, args.RowInfo?.RowData?.TransactionNo);

	private Task OnBillContextMenuItemClicked(ContextMenuClickEventArgs<BillOverviewModel> args) =>
		HandleContextMenuItem(args.Item.Id, args.RowInfo?.RowData?.TransactionNo);

	private async Task HandleContextMenuItem(string itemId, string transactionNo)
	{
		if (_isProcessing || string.IsNullOrWhiteSpace(transactionNo))
			return;

		switch (itemId)
		{
			case "View": await ViewTransaction(transactionNo); break;
			case "ExportPDF": await ExportTransaction(transactionNo); break;
			case "ExportExcel": await ExportTransaction(transactionNo, true); break;
		}
	}
	#endregion

	#region Utilities
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
