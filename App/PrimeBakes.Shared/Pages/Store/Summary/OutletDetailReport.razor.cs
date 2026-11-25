using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Accounts.FinancialAccounting;
using PrimeBakes.Exports.Inventory.Stock;
using PrimeBakes.Exports.Restaurant.Bill;
using PrimeBakes.Exports.Store.Order;
using PrimeBakes.Exports.Store.Product;
using PrimeBakes.Exports.Store.Sale;
using PrimeBakes.Exports.Store.StockTransfer;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class OutletDetailReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showDeleted = false;

	private enum RecordKind { Accounting, Order, Sale, SaleReturn, StockTransfer, Bill, Stock }

	private int _activeTabIndex;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation;

	private List<LocationModel> _locations = [];
	private List<LedgerModel> _ledgers = [];
	private List<FinancialAccountingOverviewModel> _accountings = [];
	private List<FinancialAccountingOverviewModel> _allAccountings = [];
	private List<FinancialAccountingLedgerOverviewModel> _ledgerEntries = [];
	private List<FinancialAccountingLedgerOverviewModel> _allLedgerEntries = [];
	private List<ProductLocationOverviewModel> _productLocations = [];
	private List<ProductLocationOverviewModel> _allProductLocations = [];
	private List<OrderOverviewModel> _orders = [];
	private List<OrderOverviewModel> _allOrders = [];
	private List<OrderItemOverviewModel> _orderItems = [];
	private List<OrderItemOverviewModel> _allOrderItems = [];
	private List<SaleOverviewModel> _outletPurchases = [];
	private List<SaleItemOverviewModel> _outletPurchaseItems = [];
	private List<SaleReturnOverviewModel> _outletPurchaseReturns = [];
	private List<SaleReturnItemOverviewModel> _outletPurchaseReturnItems = [];
	private List<SaleOverviewModel> _sales = [];
	private List<SaleOverviewModel> _allSales = [];
	private List<SaleItemOverviewModel> _saleItems = [];
	private List<SaleItemOverviewModel> _allSaleItems = [];
	private List<SaleReturnOverviewModel> _saleReturns = [];
	private List<SaleReturnOverviewModel> _allSaleReturns = [];
	private List<SaleReturnItemOverviewModel> _saleReturnItems = [];
	private List<SaleReturnItemOverviewModel> _allSaleReturnItems = [];
	private List<StockTransferOverviewModel> _stockTransfers = [];
	private List<StockTransferOverviewModel> _allStockTransfers = [];
	private List<StockTransferItemOverviewModel> _stockTransferItems = [];
	private List<StockTransferItemOverviewModel> _allStockTransferItems = [];
	private List<BillOverviewModel> _bills = [];
	private List<BillOverviewModel> _allBills = [];
	private List<BillItemOverviewModel> _billItems = [];
	private List<BillItemOverviewModel> _allBillItems = [];
	private List<ProductStockSummaryModel> _stockSummary = [];
	private List<ProductStockDetailsModel> _stockDetails = [];
	private List<ProductStockDetailsModel> _allStockDetails = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<FinancialAccountingOverviewModel> _accountingGrid;
	private SfGrid<FinancialAccountingLedgerOverviewModel> _ledgerGrid;
	private SfGrid<ProductLocationOverviewModel> _productLocationGrid;
	private SfGrid<OrderOverviewModel> _orderGrid;
	private SfGrid<OrderItemOverviewModel> _orderItemGrid;
	private SfGrid<SaleOverviewModel> _outletPurchaseGrid;
	private SfGrid<SaleItemOverviewModel> _outletPurchaseItemGrid;
	private SfGrid<SaleReturnOverviewModel> _outletPurchaseReturnGrid;
	private SfGrid<SaleReturnItemOverviewModel> _outletPurchaseReturnItemGrid;
	private SfGrid<SaleOverviewModel> _saleGrid;
	private SfGrid<SaleItemOverviewModel> _saleItemGrid;
	private SfGrid<SaleReturnOverviewModel> _saleReturnGrid;
	private SfGrid<SaleReturnItemOverviewModel> _saleReturnItemGrid;
	private SfGrid<StockTransferOverviewModel> _stockTransferGrid;
	private SfGrid<StockTransferItemOverviewModel> _stockTransferItemGrid;
	private SfGrid<BillOverviewModel> _billGrid;
	private SfGrid<BillItemOverviewModel> _billItemGrid;
	private SfGrid<ProductStockSummaryModel> _stockSummaryGrid;
	private SfGrid<ProductStockDetailsModel> _stockDetailGrid;
	private CustomDateRangePicker _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Admin], true, StoreRouteNames.OutletDetailReport);
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
		var currentDateTime = CommonData.LoadCurrentDateTime();
		var locations = CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location, useLocalDB: true);
		var ledgers = CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger, useLocalDB: true);

		_fromDate = _toDate = await currentDateTime;
		_locations = [.. (await locations).OrderBy(s => s.Name)];
		_ledgers = [.. await ledgers];

		_selectedLocation = _locations.First(s => s.Id == _user.LocationId);
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
			var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

			var accountings = CommonData.LoadTableDataByDate<FinancialAccountingOverviewModel>(AccountNames.FinancialAccountingOverview, fromDate, toDate, useLocalDB: true);
			var ledgerEntries = CommonData.LoadTableDataByDate<FinancialAccountingLedgerOverviewModel>(AccountNames.FinancialAccountingLedgerOverview, fromDate, toDate, useLocalDB: true);
			var productLocations = CommonData.LoadTableData<ProductLocationOverviewModel>(StoreNames.ProductLocationOverview);
			var orders = CommonData.LoadTableDataByDate<OrderOverviewModel>(StoreNames.OrderOverview, fromDate, toDate, useLocalDB: true);
			var orderItems = CommonData.LoadTableDataByDate<OrderItemOverviewModel>(StoreNames.OrderItemOverview, fromDate, toDate, useLocalDB: true);
			var sales = CommonData.LoadTableDataByDate<SaleOverviewModel>(StoreNames.SaleOverview, fromDate, toDate, useLocalDB: true);
			var saleItems = CommonData.LoadTableDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate, useLocalDB: true);
			var saleReturns = CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(StoreNames.SaleReturnOverview, fromDate, toDate, useLocalDB: true);
			var saleReturnItems = CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, fromDate, toDate, useLocalDB: true);
			var stockTransfers = CommonData.LoadTableDataByDate<StockTransferOverviewModel>(StoreNames.StockTransferOverview, fromDate, toDate, useLocalDB: true);
			var stockTransferItems = CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, fromDate, toDate, useLocalDB: true);
			var bills = CommonData.LoadTableDataByDate<BillOverviewModel>(RestaurantNames.BillOverview, fromDate, toDate, useLocalDB: true);
			var billItems = CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate, useLocalDB: true);
			var stockSummary = ProductStockData.LoadProductStockSummaryByDateLocationId(fromDate, toDate, _selectedLocation.Id, true);
			var stockDetails = CommonData.LoadTableDataByDate<ProductStockDetailsModel>(InventoryNames.ProductStockDetails, fromDate, toDate, useLocalDB: true);

			_allAccountings = await accountings;
			_allLedgerEntries = await ledgerEntries;
			_allProductLocations = await productLocations;
			_allOrders = await orders;
			_allOrderItems = await orderItems;
			_allSales = await sales;
			_allSaleItems = await saleItems;
			_allSaleReturns = await saleReturns;
			_allSaleReturnItems = await saleReturnItems;
			_allStockTransfers = await stockTransfers;
			_allStockTransferItems = await stockTransferItems;
			_allBills = await bills;
			_allBillItems = await billItems;
			_stockSummary = FilterStockSummary(await stockSummary);
			_allStockDetails = await stockDetails;

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = StoreRouteNames.OutletDetailReport,
				RecordNo = $"{_selectedLocation.Name} | {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
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
		_productLocations = [.. _allProductLocations
			.Where(t => t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.Name)
			.ThenBy(t => t.FromDate)];

		_orders = [.. _allOrders
			.Where(t => (_showDeleted || t.Status) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_orderItems = [.. _allOrderItems
			.Where(t => (_showDeleted || t.MasterStatus) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		var ledgerId = _selectedLocation.LedgerId;

		List<FinancialAccountingLedgerOverviewModel> ledgerRows = [];
		if (ledgerId > 0)
			ledgerRows = [.. _allLedgerEntries.Where(l => (_showDeleted || l.MasterStatus) && l.LedgerId == ledgerId)];

		var masterIds = ledgerRows.Select(l => l.MasterId).ToHashSet();

		_accountings = [.. _allAccountings
			.Where(t => (_showDeleted || t.Status) && masterIds.Contains(t.Id))
			.OrderBy(t => t.TransactionDateTime)];

		_ledgerEntries = [.. ledgerRows
			.Select(item =>
			{
				var referenceLedgers = _allLedgerEntries
					.Where(l => (_showDeleted || l.MasterStatus) && l.MasterId == item.MasterId && l.LedgerId != ledgerId)
					.Select(l => $"{l.LedgerName}\t({(l.Debit is > 0 ? "Dr " + l.Debit.Value.FormatIndianCurrency() : l.Credit is > 0 ? "Cr " + l.Credit.Value.FormatIndianCurrency() : "0.00")})");

				var entry = item.Clone();
				entry.LedgerName = string.Join("\n", referenceLedgers);
				return entry;
			})
			.OrderBy(t => t.TransactionDateTime)];

		_outletPurchases = [.. _allSales
			.Where(t => (_showDeleted || t.Status) && ledgerId > 0 && t.PartyId == ledgerId)
			.OrderBy(t => t.TransactionDateTime)];

		_outletPurchaseItems = [.. _allSaleItems
			.Where(t => (_showDeleted || t.MasterStatus) && ledgerId > 0 && t.PartyId == ledgerId)
			.OrderBy(t => t.TransactionDateTime)];

		_outletPurchaseReturns = [.. _allSaleReturns
			.Where(t => (_showDeleted || t.Status) && ledgerId > 0 && t.PartyId == ledgerId)
			.OrderBy(t => t.TransactionDateTime)];

		_outletPurchaseReturnItems = [.. _allSaleReturnItems
			.Where(t => (_showDeleted || t.MasterStatus) && ledgerId > 0 && t.PartyId == ledgerId)
			.OrderBy(t => t.TransactionDateTime)];

		_sales = [.. _allSales
			.Where(t => (_showDeleted || t.Status) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_saleItems = [.. _allSaleItems
			.Where(t => (_showDeleted || t.MasterStatus) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_saleReturns = [.. _allSaleReturns
			.Where(t => (_showDeleted || t.Status) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_saleReturnItems = [.. _allSaleReturnItems
			.Where(t => (_showDeleted || t.MasterStatus) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_stockTransfers = [.. _allStockTransfers
			.Where(t => (_showDeleted || t.Status) && (t.LocationId == _selectedLocation.Id || t.ToLocationId == _selectedLocation.Id))
			.OrderBy(t => t.TransactionDateTime)];

		_stockTransferItems = [.. _allStockTransferItems
			.Where(t => (_showDeleted || t.MasterStatus) && (t.LocationId == _selectedLocation.Id || t.ToLocationId == _selectedLocation.Id))
			.OrderBy(t => t.TransactionDateTime)];

		_bills = [.. _allBills
			.Where(t => (_showDeleted || t.Status) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_billItems = [.. _allBillItems
			.Where(t => (_showDeleted || t.MasterStatus) && t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)];

		_stockDetails = [.. _allStockDetails
			.Where(t => t.LocationId == _selectedLocation.Id)
			.OrderBy(t => t.TransactionDateTime)
			.ThenBy(t => t.ProductName)];
	}
	#endregion

	#region Stock
	// Same rule as the Product Stock Report: items with no opening, movement or closing stock are left out
	private static List<ProductStockSummaryModel> FilterStockSummary(List<ProductStockSummaryModel> summary) =>
		[.. summary
			.Where(s => s.OpeningStock != 0 || s.InStock != 0 || s.OutStock != 0 || s.ClosingStock != 0)
			.OrderBy(s => s.ProductName)];

	// The stock summary is built for one location, so it has to be loaded again when the outlet changes
	private async Task ReloadStockSummary()
	{
		if (_isProcessing || _selectedLocation is null)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Loading", "Fetching stock data...", ToastType.Info);

			_stockSummary = FilterStockSummary(await ProductStockData.LoadProductStockSummaryByDateLocationId(
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue),
				_selectedLocation.Id, true));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load stock: {ex.Message}", ToastType.Error);
		}
		finally
		{
			if (_stockSummaryGrid is not null) await _stockSummaryGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
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

	private async Task OnLocationChanged(LocationModel value)
	{
		_selectedLocation = value is { Id: > 0 } ? value : _locations.First(s => s.Id == 1);
		ApplyFilters();
		await RefreshGrids();
		StateHasChanged();
		await ReloadStockSummary();
	}
	#endregion

	#region Actions
	private (string TransactionNo, bool Status, RecordKind Kind)? GetSelectedRecord()
	{
		switch (_activeTabIndex)
		{
			case 0:
				var accounting = _accountingGrid?.SelectedRecords?.FirstOrDefault();
				return accounting is null ? null : (accounting.TransactionNo, accounting.Status, RecordKind.Accounting);
			case 1:
				var ledgerEntry = _ledgerGrid?.SelectedRecords?.FirstOrDefault();
				return ledgerEntry is null ? null : (ledgerEntry.TransactionNo, ledgerEntry.MasterStatus, RecordKind.Accounting);
			case 3:
				var order = _orderGrid?.SelectedRecords?.FirstOrDefault();
				return order is null ? null : (order.TransactionNo, order.Status, RecordKind.Order);
			case 4:
				var orderItem = _orderItemGrid?.SelectedRecords?.FirstOrDefault();
				return orderItem is null ? null : (orderItem.TransactionNo, orderItem.MasterStatus, RecordKind.Order);
			case 5:
				var outletPurchase = _outletPurchaseGrid?.SelectedRecords?.FirstOrDefault();
				return outletPurchase is null ? null : (outletPurchase.TransactionNo, outletPurchase.Status, RecordKind.Sale);
			case 6:
				var outletPurchaseItem = _outletPurchaseItemGrid?.SelectedRecords?.FirstOrDefault();
				return outletPurchaseItem is null ? null : (outletPurchaseItem.TransactionNo, outletPurchaseItem.MasterStatus, RecordKind.Sale);
			case 7:
				var outletPurchaseReturn = _outletPurchaseReturnGrid?.SelectedRecords?.FirstOrDefault();
				return outletPurchaseReturn is null ? null : (outletPurchaseReturn.TransactionNo, outletPurchaseReturn.Status, RecordKind.SaleReturn);
			case 8:
				var outletPurchaseReturnItem = _outletPurchaseReturnItemGrid?.SelectedRecords?.FirstOrDefault();
				return outletPurchaseReturnItem is null ? null : (outletPurchaseReturnItem.TransactionNo, outletPurchaseReturnItem.MasterStatus, RecordKind.SaleReturn);
			case 9:
				var sale = _saleGrid?.SelectedRecords?.FirstOrDefault();
				return sale is null ? null : (sale.TransactionNo, sale.Status, RecordKind.Sale);
			case 10:
				var saleItem = _saleItemGrid?.SelectedRecords?.FirstOrDefault();
				return saleItem is null ? null : (saleItem.TransactionNo, saleItem.MasterStatus, RecordKind.Sale);
			case 11:
				var saleReturn = _saleReturnGrid?.SelectedRecords?.FirstOrDefault();
				return saleReturn is null ? null : (saleReturn.TransactionNo, saleReturn.Status, RecordKind.SaleReturn);
			case 12:
				var saleReturnItem = _saleReturnItemGrid?.SelectedRecords?.FirstOrDefault();
				return saleReturnItem is null ? null : (saleReturnItem.TransactionNo, saleReturnItem.MasterStatus, RecordKind.SaleReturn);
			case 13:
				var stockTransfer = _stockTransferGrid?.SelectedRecords?.FirstOrDefault();
				return stockTransfer is null ? null : (stockTransfer.TransactionNo, stockTransfer.Status, RecordKind.StockTransfer);
			case 14:
				var stockTransferItem = _stockTransferItemGrid?.SelectedRecords?.FirstOrDefault();
				return stockTransferItem is null ? null : (stockTransferItem.TransactionNo, stockTransferItem.MasterStatus, RecordKind.StockTransfer);
			case 15:
				var bill = _billGrid?.SelectedRecords?.FirstOrDefault();
				return bill is null ? null : (bill.TransactionNo, bill.Status, RecordKind.Bill);
			case 16:
				var billItem = _billItemGrid?.SelectedRecords?.FirstOrDefault();
				return billItem is null ? null : (billItem.TransactionNo, billItem.MasterStatus, RecordKind.Bill);
			case 18:
				var stockDetail = _stockDetailGrid?.SelectedRecords?.FirstOrDefault();
				// Stock adjustments have no source transaction to open or export
				return stockDetail is null || !stockDetail.TransactionId.HasValue ? null : (stockDetail.TransactionNo, true, RecordKind.Stock);
			default:
				return null;
		}
	}

	private async Task ViewSelectedTransaction()
	{
		var record = GetSelectedRecord();
		if (_isProcessing || record is null)
			return;

		if (!record.Value.Status)
		{
			await _toastNotification.ShowAsync("Cannot View", "The selected transaction is deleted. Please recover it or download invoice.", ToastType.Warning);
			return;
		}

		try
		{
			var decodedTransactionNo = record.Value.Kind switch
			{
				RecordKind.Accounting => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false, CodeType.Accounting),
				RecordKind.Order => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false, CodeType.Order),
				RecordKind.SaleReturn => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false, CodeType.SaleReturn),
				RecordKind.StockTransfer => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false, CodeType.StockTransfer),
				RecordKind.Bill => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false, CodeType.Bill),
				_ => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, false, false)
			};
			await WindowNavigation.NavigateToRoute(decodedTransactionNo.PageRouteName);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
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

			var currentDateTime = await CommonData.LoadCurrentDateTime();
			var exportType = isExcel ? ReportExportType.Excel : ReportExportType.PDF;
			var fromDate = DateOnly.FromDateTime(_fromDate);
			var toDate = DateOnly.FromDateTime(_toDate);

			var outletParty = _ledgers.FirstOrDefault(l => l.Id == _selectedLocation.LedgerId);

			var (stream, fileName) = _activeTabIndex switch
			{
				0 => FinancialAccountingReportExport.ExportReport(
					_accountings,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					null,
					null),

				1 => FinancialAccountingReportExport.ExportLedgerReport(
					_ledgerEntries,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					null,
					outletParty,
					null),

				2 => ProductLocationExport.ExportMaster(
					_productLocations,
					_locations,
					currentDateTime,
					exportType),

				3 => OrderReportExport.ExportReport(
					_orders,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					_selectedLocation),

				4 => OrderReportExport.ExportItemReport(
					_orderItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					_selectedLocation),

				5 => SaleReportExport.ExportReport(
					_outletPurchases,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					outletParty,
					null,
					null,
					null),

				6 => SaleReportExport.ExportItemReport(
					_outletPurchaseItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					null,
					outletParty,
					null),

				7 => SaleReturnReportExport.ExportReport(
					_outletPurchaseReturns,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					outletParty,
					null,
					null,
					null),

				8 => SaleReturnReportExport.ExportItemReport(
					_outletPurchaseReturnItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					null,
					outletParty,
					null),

				9 => SaleReportExport.ExportReport(
					_sales,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					_selectedLocation,
					null),

				10 => SaleReportExport.ExportItemReport(
					_saleItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					_selectedLocation,
					null,
					null),

				11 => SaleReturnReportExport.ExportReport(
					_saleReturns,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					_selectedLocation,
					null),

				12 => SaleReturnReportExport.ExportItemReport(
					_saleReturnItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					_selectedLocation,
					null,
					null),

				13 => StockTransferReportExport.ExportReport(
					_stockTransfers,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					_selectedLocation,
					null),

				14 => StockTransferReportExport.ExportItemReport(
					_stockTransferItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					_selectedLocation,
					null),

				15 => BillReportExport.ExportReport(
					_bills,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					_selectedLocation,
					null),

				16 => BillReportExport.ExportItemReport(
					_billItems,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_showDeleted,
					false,
					null,
					null,
					null,
					_selectedLocation,
					null),

				17 => ProductStockReportExport.ExportSummaryReport(
					_stockSummary,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_showAllColumns,
					_selectedLocation),

				_ => ProductStockReportExport.ExportDetailsReport(
					_stockDetails,
					currentDateTime,
					exportType,
					fromDate,
					toDate,
					_selectedLocation)
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

	private async Task ExportSelectedTransaction(bool isExcel = false)
	{
		var record = GetSelectedRecord();
		if (_isProcessing || record is null || string.IsNullOrWhiteSpace(record.Value.TransactionNo))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = record.Value.Kind switch
			{
				RecordKind.Accounting => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel, CodeType.Accounting),
				RecordKind.Order => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel, CodeType.Order),
				RecordKind.SaleReturn => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel, CodeType.SaleReturn),
				RecordKind.StockTransfer => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel, CodeType.StockTransfer),
				RecordKind.Bill => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel, CodeType.Bill),
				_ => await DecodeCode.DecodeTransactionNo(record.Value.TransactionNo, !isExcel, isExcel)
			};
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

	#region Utilities
	private async Task OnAccountingContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnLedgerContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingLedgerOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOrderContextMenuItemClicked(ContextMenuClickEventArgs<OrderOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOrderItemContextMenuItemClicked(ContextMenuClickEventArgs<OrderItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOutletPurchaseContextMenuItemClicked(ContextMenuClickEventArgs<SaleOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOutletPurchaseItemContextMenuItemClicked(ContextMenuClickEventArgs<SaleItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOutletPurchaseReturnContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnOutletPurchaseReturnItemContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnSaleContextMenuItemClicked(ContextMenuClickEventArgs<SaleOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnSaleItemContextMenuItemClicked(ContextMenuClickEventArgs<SaleItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnSaleReturnContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnSaleReturnItemContextMenuItemClicked(ContextMenuClickEventArgs<SaleReturnItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnStockTransferContextMenuItemClicked(ContextMenuClickEventArgs<StockTransferOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnStockTransferItemContextMenuItemClicked(ContextMenuClickEventArgs<StockTransferItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnBillContextMenuItemClicked(ContextMenuClickEventArgs<BillOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnBillItemContextMenuItemClicked(ContextMenuClickEventArgs<BillItemOverviewModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task OnStockDetailContextMenuItemClicked(ContextMenuClickEventArgs<ProductStockDetailsModel> args) =>
		await HandleContextMenuItem(args.Item.Id);

	private async Task HandleContextMenuItem(string id)
	{
		switch (id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
		}
	}

	private async Task RefreshGrids()
	{
		if (_accountingGrid is not null) await _accountingGrid.Refresh();
		if (_ledgerGrid is not null) await _ledgerGrid.Refresh();
		if (_productLocationGrid is not null) await _productLocationGrid.Refresh();
		if (_orderGrid is not null) await _orderGrid.Refresh();
		if (_orderItemGrid is not null) await _orderItemGrid.Refresh();
		if (_outletPurchaseGrid is not null) await _outletPurchaseGrid.Refresh();
		if (_outletPurchaseItemGrid is not null) await _outletPurchaseItemGrid.Refresh();
		if (_outletPurchaseReturnGrid is not null) await _outletPurchaseReturnGrid.Refresh();
		if (_outletPurchaseReturnItemGrid is not null) await _outletPurchaseReturnItemGrid.Refresh();
		if (_saleGrid is not null) await _saleGrid.Refresh();
		if (_saleItemGrid is not null) await _saleItemGrid.Refresh();
		if (_saleReturnGrid is not null) await _saleReturnGrid.Refresh();
		if (_saleReturnItemGrid is not null) await _saleReturnItemGrid.Refresh();
		if (_stockTransferGrid is not null) await _stockTransferGrid.Refresh();
		if (_stockTransferItemGrid is not null) await _stockTransferItemGrid.Refresh();
		if (_billGrid is not null) await _billGrid.Refresh();
		if (_billItemGrid is not null) await _billItemGrid.Refresh();
		if (_stockSummaryGrid is not null) await _stockSummaryGrid.Refresh();
		if (_stockDetailGrid is not null) await _stockDetailGrid.Refresh();
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		await RefreshGrids();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		ApplyFilters();
		await RefreshGrids();
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
