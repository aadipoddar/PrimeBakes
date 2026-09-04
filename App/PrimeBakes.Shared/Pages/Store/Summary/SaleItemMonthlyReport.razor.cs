using PrimeBakes.Exports.Store.Summary;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;
using PrimeBakes.Models.Store.Summary;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class SaleItemMonthlyReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showAmount = false;
	private bool _showBills = false;
	private bool _showSaleReturns = false;
	private bool _showStockTransfers = false;
	private bool _showCoco = true;
	private bool _showFofo = true;
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
	private LedgerModel? _selectedParty = null;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<LocationModel> _cocoLocations = [];
	private List<LocationModel> _fofoLocations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleItemOverviewModel> _transactionOverviews = [];
	private List<SaleItemOverviewModel> _allTransactionOverviews = [];
	private List<SaleReturnItemOverviewModel> _allReturnOverviews = [];
	private List<StockTransferItemOverviewModel> _allTransferOverviews = [];
	private List<BillItemOverviewModel> _allBillOverviews = [];
	private List<SaleItemMonthlySummaryModel> _monthlySummaries = [];

	private SfGrid<SaleItemMonthlySummaryModel> _sfGrid;
	private CustomAutoComplete<ProductModel> _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Store, UserRoles.Reports], true);
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
		var parties = CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_products = [.. (await products).OrderBy(s => s.Name)];
		_productCategories = [.. (await productCategories).OrderBy(s => s.Name)];
		_locations = [.. (await locations).OrderBy(s => s.Name)];
		_companies = [.. (await companies).OrderBy(s => s.Name)];
		_parties = [.. (await parties).OrderBy(s => s.Name)];

		_selectedLocation = _user.LocationId != 1 ? _locations.FirstOrDefault(s => s.Id == _user.LocationId) : null;
		_cocoLocations = [.. _locations.Where(l => l.COCO)];
		_fofoLocations = [.. _locations.Where(l => l.FOFO)];
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

			var allTransactionOverviews = CommonData.LoadReportDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate);
			var allReturnOverviews = CommonData.LoadReportDataByDate<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, fromDate, toDate);
			var allTransferOverviews = CommonData.LoadReportDataByDate<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, fromDate, toDate);
			var allBillOverviews = CommonData.LoadReportDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate);

			_allTransactionOverviews = await allTransactionOverviews;
			_allReturnOverviews = await allReturnOverviews;
			_allTransferOverviews = await allTransferOverviews;
			_allBillOverviews = await allBillOverviews;

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = StoreRouteNames.SaleItemMonthlyReport,
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

		if (_showSaleReturns) MergeReturns();
		if (_showStockTransfers) MergeTransfers();
		if (_showBills) MergeBills();

		_transactionOverviews = [.. _transactionOverviews.Where(t =>
				(_showDeleted || t.MasterStatus) &&
				(_selectedProduct is null || _selectedProduct.Id == 0 || t.ItemId == _selectedProduct.Id) &&
				(_selectedProductCategory is null || _selectedProductCategory.Id == 0 || t.ItemCategoryId == _selectedProductCategory.Id) &&
				(_selectedLocation is null || _selectedLocation.Id == 0 || t.LocationId == _selectedLocation.Id) &&
				(_selectedCompany is null || _selectedCompany.Id == 0 || t.CompanyId == _selectedCompany.Id) &&
				(_selectedParty is null || _selectedParty.Id == 0 || t.PartyId == _selectedParty.Id) &&
				(_showCoco || !_cocoLocations.Any(l => l.Id == t.LocationId)) &&
				(_showFofo || !_fofoLocations.Any(l => l.Id == t.LocationId)))
			.OrderBy(t => t.TransactionDateTime)];

		BuildMonthlySummaries();

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void BuildMonthlySummaries()
	{
		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		_monthHeaders = SaleItemMonthlySummaryModel.BuildMonthHeaders(financialYearStart);
		_reportTitle = $"Records - {financialYearStart:MMM yyyy} to {financialYearStart.AddMonths(11):MMM yyyy} ({(_showAmount ? "Amount" : "Quantity")})";

		_monthlySummaries = [.. _transactionOverviews
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var summary = new SaleItemMonthlySummaryModel
				{
					ItemId = g.Key,
					ItemName = g.First().ItemName,
					ItemCode = g.First().ItemCode,
					ItemCategoryId = g.First().ItemCategoryId,
					ItemCategoryName = g.First().ItemCategoryName,
					TotalQuantity = g.Sum(t => t.Quantity),
					TotalAmount = g.Sum(t => t.NetTotal),
					DiscountAmount = g.Sum(t => t.DiscountAmount),
					TaxAmount = g.Sum(t => t.TotalTaxAmount),
					ReturnQuantity = -g.Where(t => t.Quantity < 0).Sum(t => t.Quantity),
					ReturnAmount = -g.Where(t => t.NetTotal < 0).Sum(t => t.NetTotal),
					TransactionCount = g.Select(t => t.MasterId).Distinct().Count(),
					LocationCount = g.Select(t => t.LocationId).Distinct().Count(),
					FirstSaleDateTime = g.Min(t => t.TransactionDateTime),
					LastSaleDateTime = g.Max(t => t.TransactionDateTime)
				};

				foreach (var transaction in g)
				{
					var monthIndex = ((transaction.TransactionDateTime.Year - financialYearStart.Year) * 12) + transaction.TransactionDateTime.Month - financialYearStart.Month;
					if (monthIndex is < 0 or > 11)
						continue;

					summary[monthIndex] += _showAmount ? transaction.NetTotal : transaction.Quantity;
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
			summary.MonthsSinceLastSale = summary.LastSaleDateTime is null
				? 0
				: ((_currentDateTime.Year - summary.LastSaleDateTime.Value.Year) * 12) + _currentDateTime.Month - summary.LastSaleDateTime.Value.Month;
		}
	}

	private static int FindMonthIndex(SaleItemMonthlySummaryModel summary, decimal value)
	{
		for (var index = 0; index < 12; index++)
			if (summary[index] == value)
				return index;

		return 0;
	}

	private void MergeReturns() =>
		_transactionOverviews.AddRange(_allReturnOverviews.Select(pr => new SaleItemOverviewModel
		{
			Id = -pr.Id,
			MasterId = pr.MasterId,
			OrderTransactionNo = null,
			CustomerId = pr.CustomerId,
			CustomerName = pr.CustomerName,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			OrderId = null,
			Remarks = pr.Remarks,
			ItemId = pr.ItemId,
			ItemName = pr.ItemName,
			ItemCode = pr.ItemCode,
			ItemCategoryId = pr.ItemCategoryId,
			ItemCategoryName = pr.ItemCategoryName,
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = pr.PartyId,
			PartyName = pr.PartyName,
			TransactionNo = pr.TransactionNo,
			TransactionDateTime = pr.TransactionDateTime,
			Quantity = -pr.Quantity,
			Rate = pr.Rate,
			ItemBaseTotal = -pr.BaseTotal,
			DiscountPercent = pr.DiscountPercent,
			DiscountAmount = -pr.DiscountAmount,
			AfterDiscount = -pr.AfterDiscount,
			CGSTPercent = pr.CGSTPercent,
			CGSTAmount = -pr.CGSTAmount,
			SGSTPercent = pr.SGSTPercent,
			SGSTAmount = -pr.SGSTAmount,
			IGSTPercent = pr.IGSTPercent,
			IGSTAmount = -pr.IGSTAmount,
			TotalTaxAmount = -pr.TotalTaxAmount,
			InclusiveTax = pr.InclusiveTax,
			Total = -pr.Total,
			NetRate = pr.NetRate,
			NetTotal = -pr.NetTotal,
			ItemRemarks = pr.Remarks,
			MasterStatus = pr.MasterStatus,
			BaseTotal = -pr.BaseTotal,
			ItemDiscountAmount = -pr.ItemDiscountAmount,
			TotalAfterItemDiscount = -pr.TotalAfterItemDiscount,
			TotalInclusiveTaxAmount = -pr.TotalInclusiveTaxAmount,
			TotalExtraTaxAmount = -pr.TotalExtraTaxAmount,
			TotalAfterTax = -pr.TotalAfterTax,
			OtherChargesPercent = pr.OtherChargesPercent,
			OtherChargesAmount = -pr.OtherChargesAmount,
			SaleDiscountPercent = pr.SaleReturnDiscountPercent,
			SaleDiscountAmount = -pr.SaleReturnDiscountAmount,
			RoundOffAmount = -pr.RoundOffAmount,
			TotalAmount = -pr.TotalAmount,
			Cash = -pr.Cash,
			Card = -pr.Card,
			UPI = -pr.UPI,
			Credit = -pr.Credit,
			PaymentModes = pr.PaymentModes,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			FinancialAccountingId = pr.FinancialAccountingId,
			FinancialAccountingTransactionNo = pr.FinancialAccountingTransactionNo,
			OrderDateTime = null,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedAt = pr.CreatedAt,
			CreatedFormFactor = pr.CreatedFormFactor,
			CreatedPlatform = pr.CreatedPlatform,
			CreatedLatitude = pr.CreatedLatitude,
			CreatedLongitude = pr.CreatedLongitude,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFormFactor = pr.LastModifiedFormFactor,
			LastModifiedPlatform = pr.LastModifiedPlatform,
			LastModifiedLatitude = pr.LastModifiedLatitude,
			LastModifiedLongitude = pr.LastModifiedLongitude,
		}));

	private void MergeTransfers() =>
		_transactionOverviews.AddRange(_allTransferOverviews.Select(pr => new SaleItemOverviewModel
		{
			Id = 0,
			MasterId = pr.MasterId,
			OrderTransactionNo = null,
			CustomerId = null,
			CustomerName = null,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			OrderId = null,
			Remarks = pr.Remarks,
			ItemId = pr.ItemId,
			ItemName = pr.ItemName,
			ItemCode = pr.ItemCode,
			ItemCategoryId = pr.ItemCategoryId,
			ItemCategoryName = pr.ItemCategoryName,
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = _locations.FirstOrDefault(l => l.Id == pr.ToLocationId)?.LedgerId,
			PartyName = _locations.FirstOrDefault(l => l.Id == pr.ToLocationId)?.Name,
			TransactionNo = pr.TransactionNo,
			TransactionDateTime = pr.TransactionDateTime,
			Quantity = pr.Quantity,
			Rate = pr.Rate,
			ItemBaseTotal = pr.BaseTotal,
			DiscountPercent = pr.DiscountPercent,
			DiscountAmount = pr.DiscountAmount,
			AfterDiscount = pr.AfterDiscount,
			CGSTPercent = pr.CGSTPercent,
			CGSTAmount = pr.CGSTAmount,
			SGSTPercent = pr.SGSTPercent,
			SGSTAmount = pr.SGSTAmount,
			IGSTPercent = pr.IGSTPercent,
			IGSTAmount = pr.IGSTAmount,
			TotalTaxAmount = pr.TotalTaxAmount,
			InclusiveTax = pr.InclusiveTax,
			Total = pr.Total,
			NetRate = pr.NetRate,
			NetTotal = pr.NetTotal,
			ItemRemarks = pr.ItemRemarks,
			MasterStatus = pr.MasterStatus,
			BaseTotal = pr.BaseTotal,
			ItemDiscountAmount = pr.ItemDiscountAmount,
			TotalAfterItemDiscount = pr.TotalAfterItemDiscount,
			TotalInclusiveTaxAmount = pr.TotalInclusiveTaxAmount,
			TotalExtraTaxAmount = pr.TotalExtraTaxAmount,
			TotalAfterTax = pr.TotalAfterTax,
			OtherChargesPercent = pr.OtherChargesPercent,
			OtherChargesAmount = pr.OtherChargesAmount,
			SaleDiscountPercent = pr.StockTransferDiscountPercent,
			SaleDiscountAmount = pr.StockTransferDiscountAmount,
			RoundOffAmount = pr.RoundOffAmount,
			TotalAmount = pr.TotalAmount,
			Cash = pr.Cash,
			Card = pr.Card,
			UPI = pr.UPI,
			Credit = pr.Credit,
			PaymentModes = pr.PaymentModes,
			TotalItems = pr.TotalItems,
			TotalQuantity = pr.TotalQuantity,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			FinancialAccountingId = pr.FinancialAccountingId,
			FinancialAccountingTransactionNo = pr.FinancialAccountingTransactionNo,
			OrderDateTime = null,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedAt = pr.CreatedAt,
			CreatedFormFactor = pr.CreatedFormFactor,
			CreatedPlatform = pr.CreatedPlatform,
			CreatedLatitude = pr.CreatedLatitude,
			CreatedLongitude = pr.CreatedLongitude,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFormFactor = pr.LastModifiedFormFactor,
			LastModifiedPlatform = pr.LastModifiedPlatform,
			LastModifiedLatitude = pr.LastModifiedLatitude,
			LastModifiedLongitude = pr.LastModifiedLongitude
		}));

	private void MergeBills() =>
		_transactionOverviews.AddRange(_allBillOverviews.Select(pr => new SaleItemOverviewModel
		{
			Id = pr.Id,
			MasterId = pr.MasterId,
			OrderTransactionNo = null,
			CustomerId = pr.CustomerId,
			CustomerName = pr.CustomerName,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			OrderId = null,
			Remarks = pr.Remarks,
			ItemId = pr.ItemId,
			ItemName = pr.ItemName,
			ItemCode = pr.ItemCode,
			ItemCategoryId = pr.ItemCategoryId,
			ItemCategoryName = pr.ItemCategoryName,
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = null,
			PartyName = null,
			TransactionNo = pr.TransactionNo,
			TransactionDateTime = pr.TransactionDateTime,
			Quantity = pr.Quantity,
			Rate = pr.Rate,
			ItemBaseTotal = pr.ItemBaseTotal,
			DiscountPercent = pr.DiscountPercent,
			DiscountAmount = pr.DiscountAmount,
			AfterDiscount = pr.AfterDiscount,
			CGSTPercent = pr.CGSTPercent,
			CGSTAmount = pr.CGSTAmount,
			SGSTPercent = pr.SGSTPercent,
			SGSTAmount = pr.SGSTAmount,
			IGSTPercent = pr.IGSTPercent,
			IGSTAmount = pr.IGSTAmount,
			TotalTaxAmount = pr.TotalTaxAmount,
			InclusiveTax = pr.InclusiveTax,
			Total = pr.Total,
			NetRate = pr.NetRate,
			NetTotal = pr.NetTotal,
			ItemRemarks = pr.ItemRemarks,
			MasterStatus = pr.MasterStatus,
			BaseTotal = pr.BaseTotal,
			ItemDiscountAmount = pr.ItemDiscountAmount,
			TotalAfterItemDiscount = pr.TotalAfterItemDiscount,
			TotalInclusiveTaxAmount = pr.TotalInclusiveTaxAmount,
			TotalExtraTaxAmount = pr.TotalExtraTaxAmount,
			TotalAfterTax = pr.TotalAfterTax,
			// Bill has no "other charges"/sale discount; map its Service Charge + bill-level discount onto those columns.
			OtherChargesPercent = pr.ServiceChargePercent,
			OtherChargesAmount = pr.ServiceChargeAmount,
			SaleDiscountPercent = pr.BillDiscountPercent,
			SaleDiscountAmount = pr.BillDiscountAmount,
			RoundOffAmount = pr.RoundOffAmount,
			TotalAmount = pr.TotalAmount,
			Cash = pr.Cash,
			Card = pr.Card,
			UPI = pr.UPI,
			Credit = pr.Credit,
			PaymentModes = pr.PaymentModes,
			TotalItems = pr.TotalItems,
			TotalQuantity = pr.TotalQuantity,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			FinancialAccountingId = pr.FinancialAccountingId,
			FinancialAccountingTransactionNo = pr.FinancialAccountingTransactionNo,
			OrderDateTime = null,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedAt = pr.CreatedAt,
			CreatedFormFactor = pr.CreatedFormFactor,
			CreatedPlatform = pr.CreatedPlatform,
			CreatedLatitude = pr.CreatedLatitude,
			CreatedLongitude = pr.CreatedLongitude,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFormFactor = pr.LastModifiedFormFactor,
			LastModifiedPlatform = pr.LastModifiedPlatform,
			LastModifiedLatitude = pr.LastModifiedLatitude,
			LastModifiedLongitude = pr.LastModifiedLongitude
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

	private async Task OnPartyChanged(LedgerModel value)
	{
		_selectedParty = value;
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

			var (stream, fileName) = SaleItemMonthlyReportExport.ExportReport(
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
				_selectedLocation?.Id > 0 ? _selectedLocation : null,
				_selectedParty?.Id > 0 ? _selectedParty : null
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

	private async Task ToggleSaleReturns()
	{
		_showSaleReturns = !_showSaleReturns;
		await ApplyFilters();
	}

	private async Task ToggleStockTransfers()
	{
		_showStockTransfers = !_showStockTransfers;
		await ApplyFilters();
	}

	private async Task ToggleBills()
	{
		_showBills = !_showBills;
		await ApplyFilters();
	}

	private async Task ToggleCoco()
	{
		_showCoco = !_showCoco;
		await ApplyFilters();
	}

	private async Task ToggleFofo()
	{
		_showFofo = !_showFofo;
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
