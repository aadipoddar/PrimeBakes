using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Inventory.Summary;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class PurchaseOrderItemMonthlyReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showPendingOnly = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;
	private DateTime _currentDateTime = DateTime.Now;

	private string _reportTitle = "Records";
	private readonly List<string> _monthFields = [.. Enumerable.Range(1, 12).Select(month => $"Month{month}")];
	private List<string> _monthHeaders = [.. Enumerable.Repeat(string.Empty, 12)];

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
	private List<PurchaseOrderItemMonthlySummaryModel> _monthlySummaries = [];

	private SfGrid<PurchaseOrderItemMonthlySummaryModel> _sfGrid;
	private CustomAutoComplete<RawMaterialModel> _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory, UserRoles.Reports], true);
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

		var rawMaterials = CommonData.LoadTableDataByStatus<RawMaterialModel>(InventoryNames.RawMaterial);
		var rawMaterialCategories = CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);
		var companies = CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		var parties = CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

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

			_currentDateTime = await CommonData.LoadCurrentDateTime();

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<PurchaseOrderItemOverviewModel>(
				InventoryNames.PurchaseOrderItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = InventoryRouteNames.PurchaseOrderItemMonthlyReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform()
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
				(!_showPendingOnly || t.PurchaseId is null) &&
				(_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || t.ItemId == _selectedRawMaterial.Id) &&
				(_selectedRawMaterialCategory is null || _selectedRawMaterialCategory.Id == 0 || t.ItemCategoryId == _selectedRawMaterialCategory.Id) &&
				(_selectedCompany is null || _selectedCompany.Id == 0 || t.CompanyId == _selectedCompany.Id) &&
				(_selectedParty is null || _selectedParty.Id == 0 || t.PartyId == _selectedParty.Id))
			.OrderBy(t => t.TransactionDateTime)];

		BuildMonthlySummaries();

		if (_sfGrid is not null) await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void BuildMonthlySummaries()
	{
		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		_monthHeaders = PurchaseOrderItemMonthlySummaryModel.BuildMonthHeaders(financialYearStart);
		_reportTitle = $"Records - {financialYearStart:MMM yyyy} to {financialYearStart.AddMonths(11):MMM yyyy}";

		_monthlySummaries = [.. _transactionOverviews
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var summary = new PurchaseOrderItemMonthlySummaryModel
				{
					ItemId = g.Key,
					ItemName = g.First().ItemName,
					ItemCode = g.First().ItemCode,
					ItemCategoryId = g.First().ItemCategoryId,
					ItemCategoryName = g.First().ItemCategoryName,
					UnitOfMeasurement = g.First().UnitOfMeasurement,
					FulfilledQuantity = g.Where(t => t.PurchaseId is not null).Sum(t => t.Quantity),
					PendingQuantity = g.Where(t => t.PurchaseId is null).Sum(t => t.Quantity),
					OrderCount = g.Select(t => t.MasterId).Distinct().Count(),
					FulfilledOrderCount = g.Where(t => t.PurchaseId is not null).Select(t => t.MasterId).Distinct().Count(),
					PartyCount = g.Select(t => t.PartyId).Distinct().Count(),
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

	private static int FindMonthIndex(PurchaseOrderItemMonthlySummaryModel summary, decimal value)
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

			var (stream, fileName) = PurchaseOrderItemMonthlyReportExport.ExportReport(
				_monthlySummaries,
				_monthHeaders,
				_currentDateTime,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
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
	#endregion

	#region Utilities
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
