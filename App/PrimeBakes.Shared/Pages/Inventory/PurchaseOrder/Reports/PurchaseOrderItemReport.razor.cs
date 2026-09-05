using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Inventory.PurchaseOrder;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Inventory.PurchaseOrder;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Inventory.PurchaseOrder.Reports;

public partial class PurchaseOrderItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showPendingOnly = false;
	private bool _showDeleted = false;
	private double _locationRadius = 1000;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private RawMaterialModel _selectedRawMaterial = null;
	private RawMaterialCategoryModel _selectedRawMaterialCategory = null;
	private CompanyModel _selectedCompany = null;
	private LedgerModel _selectedParty = null;

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseOrderItemOverviewModel> _transactionOverviews = [];
	private List<PurchaseOrderItemOverviewModel> _allTransactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "View Linked Purchase", Id = "ViewPurchase", IconCss = "e-icons e-link", Target = ".e-content" },
		new() { Text = "Export PDF (Alt + P)", Id = "ExportPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Export Excel (Alt + E)", Id = "ExportExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" },
		new() { Text = "Open Created Location", Id = "OpenCreatedLocation", IconCss = "e-icons e-location", Target = ".e-content" },
		new() { Text = "Open Modified Location", Id = "OpenModifiedLocation", IconCss = "e-icons e-location", Target = ".e-content" }
	];

	private SfGrid<PurchaseOrderItemOverviewModel> _sfGrid;
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
		var rawMaterials = CommonData.LoadTableDataByStatus<RawMaterialModel>(InventoryNames.RawMaterial);
		var rawMaterialCategories = CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);
		var companies = CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		var parties = CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_fromDate = _toDate = await currentDateTime;
		_rawMaterials = [.. (await rawMaterials).OrderBy(s => s.Name)];
		_rawMaterialCategories = [.. (await rawMaterialCategories).OrderBy(s => s.Name)];
		_companies = [.. (await companies).OrderBy(s => s.Name)];
		_parties = [.. (await parties).OrderBy(s => s.Name)];
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

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<PurchaseOrderItemOverviewModel>(
				InventoryNames.PurchaseOrderItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue), useLocalDB: true);

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = InventoryRouteNames.PurchaseOrderItemReport,
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
		_transactionOverviews = [.. _allTransactionOverviews.Where(t =>
				(_showDeleted || t.MasterStatus) &&
				(!_showPendingOnly || t.PurchaseId is null) &&
				(_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || t.ItemId == _selectedRawMaterial.Id) &&
				(_selectedRawMaterialCategory is null || _selectedRawMaterialCategory.Id == 0 || t.ItemCategoryId == _selectedRawMaterialCategory.Id) &&
				(_selectedCompany is null || _selectedCompany.Id == 0 || t.CompanyId == _selectedCompany.Id) &&
				(_selectedParty is null || _selectedParty.Id == 0 || t.PartyId == _selectedParty.Id))
			.OrderBy(t => t.TransactionDateTime)];

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => t.ItemName)
				.Select(g => new PurchaseOrderItemOverviewModel
				{
					ItemName = g.Key,
					ItemCode = g.First().ItemCode,
					ItemCategoryName = g.First().ItemCategoryName,
					UnitOfMeasurement = g.First().UnitOfMeasurement,
					Quantity = g.Sum(t => t.Quantity)
				})
				.OrderBy(t => t.ItemName)];

		if (_sfGrid is not null) await _sfGrid.Refresh();
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

	private async Task OnRawMaterialChanged(RawMaterialModel value)
	{
		_selectedRawMaterial = value;
		await ApplyFilters();
	}

	private async Task OnRawMaterialCategoryChanged(RawMaterialCategoryModel value)
	{
		_selectedRawMaterialCategory = value;
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
	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		if (!_sfGrid.SelectedRecords.First().MasterStatus)
		{
			await _toastNotification.ShowAsync("Cannot View", "The selected transaction is deleted. Please recover it first.", ToastType.Warning);
			return;
		}

		var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, false, false, CodeType.PurchaseOrder);
		await WindowNavigation.NavigateToRoute(decodedTransactionNo.PageRouteName);
	}

	private async Task ViewLinkedPurchase()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();
		if (string.IsNullOrWhiteSpace(record.PurchaseTransactionNo))
		{
			await _toastNotification.ShowAsync("No Purchase", "This purchase order has not been received yet.", ToastType.Warning);
			return;
		}

		var decoded = await DecodeCode.DecodeTransactionNo(record.PurchaseTransactionNo, false, false, CodeType.Purchase);
		await WindowNavigation.NavigateToRoute(decoded.PageRouteName);
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

			var purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, masterId)
				?? throw new Exception("Transaction not found.");

			purchaseOrder.Status = isRecover;
			purchaseOrder.LastModifiedBy = _user.Id;
			purchaseOrder.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			var platform = await PlatformInfo.GetPlatformInfo();
			purchaseOrder.LastModifiedFormFactor = platform.FormFactor;
			purchaseOrder.LastModifiedPlatform = platform.Platform;
			purchaseOrder.LastModifiedLatitude = platform.Latitude;
			purchaseOrder.LastModifiedLongitude = platform.Longitude;

			if (isRecover) await PurchaseOrderData.RecoverTransaction(purchaseOrder);
			else await PurchaseOrderData.DeleteTransaction(purchaseOrder);

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

			var (stream, fileName) = PurchaseOrderReportExport.ExportItemReport(
				_transactionOverviews,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_showSummary,
				_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial : null,
				_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory : null,
				_selectedCompany?.Id > 0 ? _selectedCompany : null,
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
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, !isExcel, isExcel, CodeType.PurchaseOrder);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<PurchaseOrderItemOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "ViewPurchase": await ViewLinkedPurchase(); break;
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

	private async Task TogglePendingOnly()
	{
		_showPendingOnly = !_showPendingOnly;
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

		var radiusSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.LocationRadiusMeters);
		_locationRadius = double.TryParse(radiusSetting?.Value, out var radius) ? radius : 1000;

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
