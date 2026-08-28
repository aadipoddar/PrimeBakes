using PrimeBakes.Exports.Inventory.Kitchen;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenProduction.Reports;

public partial class KitchenProductionItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showDeleted = false;
	private bool _showTransactionReturns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

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

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" },
		new() { Text = "Open Created Location", Id = "OpenCreatedLocation", IconCss = "e-icons e-location", Target = ".e-content" },
		new() { Text = "Open Modified Location", Id = "OpenModifiedLocation", IconCss = "e-icons e-location", Target = ".e-content" }
	];

	private SfGrid<KitchenProductionItemOverviewModel> _sfGrid;
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
		var currentDateTime = CommonData.LoadCurrentDateTime();
		var products = CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		var productCategories = CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		var companies = CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		var kitchens = CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen);

		_fromDate = _toDate = await currentDateTime;
		_products = await products;
		_productCategories = await productCategories;
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

			var allTransactionOverviews = CommonData.LoadTableDataByDate<KitchenProductionItemOverviewModel>(
				InventoryNames.KitchenProductionItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));
			var allTransactionReturnOverviews = CommonData.LoadTableDataByDate<KitchenProductionReturnItemOverviewModel>(
				InventoryNames.KitchenProductionReturnItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			_allTransactionOverviews = await allTransactionOverviews;
			_allTransactionReturnOverviews = await allTransactionReturnOverviews;

			var platform = await AuthService.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = InventoryRouteNames.KitchenProductionItemReport,
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

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => t.ItemName)
				.Select(g => new KitchenProductionItemOverviewModel
				{
					ItemName = g.Key,
					ItemCode = g.First().ItemCode,
					ItemCategoryName = g.First().ItemCategoryName,
					Quantity = g.Sum(t => t.Quantity),
					Total = g.Sum(t => t.Total)
				})
				.OrderBy(t => t.ItemName)];

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
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

	#region Actions
	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		if (!_sfGrid.SelectedRecords.First().MasterStatus)
		{
			await _toastNotification.ShowAsync("Cannot View", "The selected transaction is deleted. Please recover it or download invoice.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false);
		await AuthenticationService.NavigateToRoute(decodedTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task DeleteRecoverTransaction(int masterId, string transactionNo, bool isRecover)
	{
		if (_isProcessing || masterId == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission for the action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var platform = await AuthService.GetPlatformInfo();
			var currentDateTime = await CommonData.LoadCurrentDateTime();

			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(transactionNo, false, false);

			if (decodedTransactionNo.CodeType == CodeType.KitchenProductionReturn)
			{
				var kitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, masterId)
					?? throw new Exception("Transaction not found.");
				kitchenProductionReturn.Status = isRecover;
				kitchenProductionReturn.LastModifiedBy = _user.Id;
				kitchenProductionReturn.LastModifiedAt = currentDateTime;
				kitchenProductionReturn.LastModifiedFormFactor = platform.FormFactor;
				kitchenProductionReturn.LastModifiedPlatform = platform.Platform;
				kitchenProductionReturn.LastModifiedLatitude = platform.Latitude;
				kitchenProductionReturn.LastModifiedLongitude = platform.Longitude;

				if (isRecover) await KitchenProductionReturnData.RecoverTransaction(kitchenProductionReturn);
				else await KitchenProductionReturnData.DeleteTransaction(kitchenProductionReturn);
			}
			else
			{
				var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(InventoryNames.KitchenProduction, masterId)
					?? throw new Exception("Transaction not found.");
				kitchenProduction.Status = isRecover;
				kitchenProduction.LastModifiedBy = _user.Id;
				kitchenProduction.LastModifiedAt = currentDateTime;
				kitchenProduction.LastModifiedFormFactor = platform.FormFactor;
				kitchenProduction.LastModifiedPlatform = platform.Platform;
				kitchenProduction.LastModifiedLatitude = platform.Latitude;
				kitchenProduction.LastModifiedLongitude = platform.Longitude;

				if (isRecover) await KitchenProductionData.RecoverTransaction(kitchenProduction);
				else await KitchenProductionData.DeleteTransaction(kitchenProduction);
			}

			await _toastNotification.ShowAsync("Success", $"Transaction {transactionNo} has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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

	private async Task DeleteRecoverSelectedTransaction()
	{
		if (_showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		await ShowConfirmation(record.MasterStatus ? "Delete" : "Recover",
			$"Are you sure you want to {(record.MasterStatus ? "delete" : "recover")} transaction {record.TransactionNo}",
			() => DeleteRecoverTransaction(record.MasterId, record.TransactionNo, !record.MasterStatus));
	}

	private async Task OpenSelectedTransactionLocation(bool lastModified = false)
	{
		var record = _sfGrid?.SelectedRecords?.FirstOrDefault();
		await LocationService.OpenMapAsync(
			lastModified ? record?.LastModifiedLatitude : record?.CreatedLatitude,
			lastModified ? record?.LastModifiedLongitude : record?.CreatedLongitude);
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

			var (stream, fileName) = KitchenProductionReportExport.ExportItemReport(
				_transactionOverviews,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_showSummary,
				_selectedProduct?.Id > 0 ? _selectedProduct : null,
				_selectedProductCategory?.Id > 0 ? _selectedProductCategory : null,
				_selectedKitchen?.Id > 0 ? _selectedKitchen : null,
				_selectedCompany?.Id > 0 ? _selectedCompany : null
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
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, !isExcel, isExcel);
			await SaveAndViewService.SaveAndView(
				isExcel ? decodeTransactionNo.ExcelStream.fileName : decodeTransactionNo.PDFStream.fileName,
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenProductionItemOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ExportPDF": await ExportSelectedTransaction(); break;
			case "ExportExcel": await ExportSelectedTransaction(true); break;
			case "DeleteRecover": await DeleteRecoverSelectedTransaction(); break;
			case "OpenCreatedLocation": await OpenSelectedTransactionLocation(); break;
			case "OpenModifiedLocation": await OpenSelectedTransactionLocation(true); break;
		}
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

	private async Task ToggleSummary()
	{
		_showSummary = !_showSummary;
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
