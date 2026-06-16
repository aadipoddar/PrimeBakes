using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Restaurant.Bill.Data;
using PrimeBakesLibrary.Restaurant.Bill.Exports;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Customer;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Utils.Exports;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Reports;

public partial class BillItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private ProductModel? _selectedProduct = null;
	private ProductCategoryModel? _selectedProductCategory = null;
	private LocationModel? _selectedLocation = null;
	private CompanyModel? _selectedCompany = null;
	private CustomerModel? _selectedCustomer = null;

	private int _runningFilter = YesNoFilterOptions.All;
	private YesNoFilterOption _selectedRunning;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<CustomerModel> _customers = [];
	private List<BillItemOverviewModel> _transactionOverviews = [];
	private List<BillItemOverviewModel> _allTransactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "View Accounting Posting", Id = "ViewAccountingPosting", IconCss = "e-icons e-link", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<BillItemOverviewModel> _sfGrid;
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
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant, UserRoles.Reports], true);
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
		_customers = await CommonData.LoadTableData<CustomerModel>(StoreNames.Customer);

		_products = [.. _products.OrderBy(s => s.Name)];
		_productCategories = [.. _productCategories.OrderBy(s => s.Name)];
		_locations = [.. _locations.OrderBy(s => s.Name)];
		_companies = [.. _companies.OrderBy(s => s.Name)];
		_customers = [.. _customers.OrderBy(s => s.Name)];

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

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate);

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
				(_selectedCustomer is null || _selectedCustomer.Id == 0 || t.CustomerId == _selectedCustomer.Id) &&
				(_runningFilter == YesNoFilterOptions.All ||
					(_runningFilter == YesNoFilterOptions.Yes && t.Running) ||
					(_runningFilter == YesNoFilterOptions.No && !t.Running)))
			.OrderBy(t => t.TransactionDateTime)];

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => t.ItemName)
				.Select(g => new BillItemOverviewModel
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

	private async Task OnCustomerChanged(CustomerModel value)
	{
		_selectedCustomer = value;
		await ApplyFilters();
	}

	private async Task OnRunningChanged(YesNoFilterOption value)
	{
		_selectedRunning = value;
		_runningFilter = value?.Id ?? YesNoFilterOptions.All;
		await ApplyFilters();
	}
	#endregion

	#region Actions
	private async Task PostDaywiseBills()
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
			await _toastNotification.ShowAsync("Processing", "Closing day and posting bill accounting...", ToastType.Info);

			for (var date = _fromDate; date <= _toDate; date = date.AddDays(1))
				await BillData.PostDayBills(
					date,
					_selectedLocation.Id,
					_user.Id,
					FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false, CodeType.Bill);
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

	private async Task DeleteRecoverSelectedTransaction()
	{
		if (_showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		await ShowConfirmation(record.MasterStatus ? "Delete" : "Recover",
			$"Are you sure you want to {(record.MasterStatus ? "delete" : "recover")} transaction {record.TransactionNo}",
			() => DeleteRecoverTransaction(record, !record.MasterStatus));
	}

	private async Task DeleteRecoverTransaction(BillItemOverviewModel record, bool isRecover)
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

			var platform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			var currentDateTime = await CommonData.LoadCurrentDateTime();

			var bill = await CommonData.LoadTableDataById<BillModel>(RestaurantNames.Bill, record.MasterId)
				?? throw new Exception("Transaction not found.");
			bill.Status = isRecover;
			bill.LastModifiedBy = _user.Id;
			bill.LastModifiedAt = currentDateTime;
			bill.LastModifiedFromPlatform = platform;

			if (isRecover) await BillData.RecoverTransaction(bill);
			else await BillData.DeleteTransaction(bill);

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

			var (stream, fileName) = await BillReportExport.ExportItemReport(
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

	private async Task ExportSelectedTransaction(bool isExcel = false)
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0 || string.IsNullOrWhiteSpace(_sfGrid.SelectedRecords.First().TransactionNo))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, !isExcel, isExcel, CodeType.Bill);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<BillItemOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ViewAccountingPosting": await ViewFinancialAccountingPosting(); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
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
