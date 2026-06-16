using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Restaurant.Bill.Data;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Data;
using PrimeBakesLibrary.Store.Sale.Exports;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Store.StockTransfer.Data;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSaleReturns = false;
	private bool _showStockTransfers = false;
	private bool _showBills = false;
	private bool _showSummary = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private ProductModel? _selectedProduct = null;
	private ProductCategoryModel? _selectedProductCategory = null;
	private LocationModel? _selectedLocation = null;
	private CompanyModel? _selectedCompany = null;
	private LedgerModel? _selectedParty = null;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleItemOverviewModel> _transactionOverviews = [];
	private List<SaleItemOverviewModel> _allTransactionOverviews = [];
	private List<SaleReturnItemOverviewModel> _allReturnOverviews = [];
	private List<StockTransferItemOverviewModel> _allTransferOverviews = [];
	private List<BillItemOverviewModel> _allBillOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "View Order (Alt + S)", Id = "ViewOrder", IconCss = "e-icons e-link", Target = ".e-content" },
		new() { Text = "View Accounting Posting", Id = "ViewAccountingPosting", IconCss = "e-icons e-link", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Export Order PDF", Id = "ExportOrderPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Order Excel", Id = "ExportOrderExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<SaleItemOverviewModel> _sfGrid;
	private CustomDateRangePicker _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

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

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_products = [.. _products.OrderBy(s => s.Name)];
		_productCategories = [.. _productCategories.OrderBy(s => s.Name)];
		_locations = [.. _locations.OrderBy(s => s.Name)];
		_companies = [.. _companies.OrderBy(s => s.Name)];
		_parties = [.. _parties.OrderBy(s => s.Name)];

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

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate);
			_allReturnOverviews = await CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, fromDate, toDate);
			_allTransferOverviews = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, fromDate, toDate);
			_allBillOverviews = await CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate);

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
				(_selectedParty is null || _selectedParty.Id == 0 || t.PartyId == _selectedParty.Id))
			.OrderBy(t => t.TransactionDateTime)];

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => t.ItemName)
				.Select(g => new SaleItemOverviewModel
				{
					ItemName = g.Key,
					ItemCode = g.First().ItemCode,
					ItemCategoryName = g.First().ItemCategoryName,
					Quantity = g.Sum(t => t.Quantity),
					ItemBaseTotal = g.Sum(t => t.ItemBaseTotal),
					DiscountAmount = g.Sum(t => t.DiscountAmount),
					AfterDiscount = g.Sum(t => t.AfterDiscount),
					SGSTAmount = g.Sum(t => t.SGSTAmount),
					CGSTAmount = g.Sum(t => t.CGSTAmount),
					IGSTAmount = g.Sum(t => t.IGSTAmount),
					TotalTaxAmount = g.Sum(t => t.TotalTaxAmount),
					Total = g.Sum(t => t.Total),
					NetTotal = g.Sum(t => t.NetTotal)
				})
				.OrderBy(t => t.ItemName)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
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
			CreatedFromPlatform = pr.CreatedFromPlatform,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
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
			CreatedFromPlatform = pr.CreatedFromPlatform,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform
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
			CreatedFromPlatform = pr.CreatedFromPlatform,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform
		}));
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

	#region Actions
	private async Task PostDaywiseSales()
	{
		if (_isProcessing)
			return;

		if (_user.LocationId != 1 || _selectedLocation is null || _selectedLocation.Id <= 1)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a specific location before day closing.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Closing day and posting sale accounting...", ToastType.Info);

			for (var date = _fromDate; date <= _toDate; date = date.AddDays(1))
			{
				await SaleData.PostDaySales(
					date,
					_selectedLocation.Id,
					_user.Id,
					FormFactor.GetFormFactor() + FormFactor.GetPlatform());

				await BillData.PostDayBills(
					date,
					_selectedLocation.Id,
					_user.Id,
					FormFactor.GetFormFactor() + FormFactor.GetPlatform());
			}

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

	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		if (!_sfGrid.SelectedRecords.First().MasterStatus)
		{
			await _toastNotification.ShowAsync("Cannot View", "The selected transaction is deleted. Please recover it or download invoice.", ToastType.Warning);
			return;
		}

		try
		{
			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false);
			await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
	}

	private async Task ViewFinancialAccountingPosting()
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		if (string.IsNullOrWhiteSpace(record.FinancialAccountingTransactionNo))
		{
			await _toastNotification.ShowAsync("No Posting", "This transaction has no linked accounting posting.", ToastType.Warning);
			return;
		}

		var decoded = await DecodeCode.DecodeTransactionNo(record.FinancialAccountingTransactionNo, false, false, CodeType.Accounting);
		await AuthenticationService.NavigateToRoute(decoded.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task ViewSelectedOrderTransaction()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		if (string.IsNullOrWhiteSpace(record.OrderTransactionNo))
		{
			await _toastNotification.ShowAsync("No Order", "This sale has no linked order.", ToastType.Warning);
			return;
		}

		try
		{
			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(record.OrderTransactionNo, false, false, CodeType.Order);
			await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening order: {ex.Message}", ToastType.Error);
		}
	}

	private async Task DeleteRecoverSelectedTransaction()
	{
		if (_showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		await ShowConfirmation(record.MasterStatus ? "Delete" : "Recover",
			$"Are you sure you want to {(record.MasterStatus ? "delete" : "recover")} transaction {record.TransactionNo}",
			() => DeleteRecoverTransaction(record, !record.MasterStatus));
	}

	private async Task DeleteRecoverTransaction(SaleItemOverviewModel record, bool isRecover)
	{
		if (_isProcessing || record is null || record.MasterId == 0)
			return;

		try
		{
			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission for the action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(record.TransactionNo, false, false);
			var platform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			var currentDateTime = await CommonData.LoadCurrentDateTime();

			if (decodedTransactionNo.CodeType == CodeType.Sale)
			{
				var sale = await CommonData.LoadTableDataById<SaleModel>(StoreNames.Sale, record.MasterId)
					?? throw new Exception("Transaction not found.");
				sale.Status = isRecover;
				sale.LastModifiedBy = _user.Id;
				sale.LastModifiedAt = currentDateTime;
				sale.LastModifiedFromPlatform = platform;

				if (isRecover) await SaleData.RecoverTransaction(sale);
				else await SaleData.DeleteTransaction(sale);
			}
			else if (decodedTransactionNo.CodeType == CodeType.SaleReturn)
			{
				var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(StoreNames.SaleReturn, record.MasterId)
						?? throw new Exception("Transaction not found.");
				saleReturn.Status = isRecover;
				saleReturn.LastModifiedBy = _user.Id;
				saleReturn.LastModifiedAt = currentDateTime;
				saleReturn.LastModifiedFromPlatform = platform;

				if (isRecover) await SaleReturnData.RecoverTransaction(saleReturn);
				else await SaleReturnData.DeleteTransaction(saleReturn);
			}
			else if (decodedTransactionNo.CodeType == CodeType.StockTransfer)
			{
				var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(StoreNames.StockTransfer, record.MasterId)
						?? throw new Exception("Transaction not found.");
				stockTransfer.Status = isRecover;
				stockTransfer.LastModifiedBy = _user.Id;
				stockTransfer.LastModifiedAt = currentDateTime;
				stockTransfer.LastModifiedFromPlatform = platform;

				if (isRecover) await StockTransferData.RecoverTransaction(stockTransfer);
				else await StockTransferData.DeleteTransaction(stockTransfer);
			}

			await _toastNotification.ShowAsync("Success", $"Transaction {record.TransactionNo} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while {(isRecover ? "recovering" : "deleting")} transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
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

			var (stream, fileName) = await SaleReportExport.ExportItemReport(
				_transactionOverviews,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_showSummary,
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

	private async Task ExportSelectedTransaction(bool isExcel = false)
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrWhiteSpace(_sfGrid.SelectedRecords.First().TransactionNo))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, !isExcel, isExcel);
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

	private async Task ExportSelectedOrderTransaction(bool isExcel = false)
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		if (string.IsNullOrWhiteSpace(record.OrderTransactionNo))
		{
			await _toastNotification.ShowAsync("No Order", "This sale has no linked order.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(record.OrderTransactionNo, !isExcel, isExcel, CodeType.Order);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<SaleItemOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ViewOrder": await ViewSelectedOrderTransaction(); break;
			case "ViewAccountingPosting": await ViewFinancialAccountingPosting(); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
			case "ExportOrderPDF": await ExportSelectedOrderTransaction(); break;
			case "ExportOrderExcel": await ExportSelectedOrderTransaction(true); break;
			case "DeleteRecover": await DeleteRecoverSelectedTransaction(); break;
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
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

	private async Task ToggleSummary()
	{
		_showSummary = !_showSummary;
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
