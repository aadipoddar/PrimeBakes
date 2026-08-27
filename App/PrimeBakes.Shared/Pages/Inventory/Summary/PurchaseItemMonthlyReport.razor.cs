using PrimeBakes.Exports.Inventory.Summary;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Inventory.Summary;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class PurchaseItemMonthlyReport : IAsyncDisposable
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

	private RawMaterialModel? _selectedRawMaterial = null;
	private RawMaterialCategoryModel? _selectedRawMaterialCategory = null;
	private CompanyModel? _selectedCompany = null;
	private LedgerModel? _selectedParty = null;

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseItemOverviewModel> _transactionOverviews = [];
	private List<PurchaseItemOverviewModel> _allTransactionOverviews = [];
	private List<PurchaseReturnItemOverviewModel> _allTransactionReturnOverviews = [];
	private List<PurchaseItemMonthlySummaryModel> _monthlySummaries = [];

	private SfGrid<PurchaseItemMonthlySummaryModel> _sfGrid;
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

			var fromDate = DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue);
			var toDate = DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue);

			var allTransactionOverviews = CommonData.LoadTableDataByDate<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, fromDate, toDate);
			var allTransactionReturnOverviews = CommonData.LoadTableDataByDate<PurchaseReturnItemOverviewModel>(InventoryNames.PurchaseReturnItemOverview, fromDate, toDate);

			_allTransactionOverviews = await allTransactionOverviews;
			_allTransactionReturnOverviews = await allTransactionReturnOverviews;

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = InventoryRouteNames.PurchaseItemMonthlyReport,
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
		_transactionOverviews = [.. _allTransactionOverviews];

		if (_showTransactionReturns)
			MergeTransactionAndReturns();

		_transactionOverviews = [.. _transactionOverviews.Where(t =>
				(_showDeleted || t.MasterStatus) &&
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
		_monthHeaders = PurchaseItemMonthlySummaryModel.BuildMonthHeaders(financialYearStart);
		_reportTitle = $"Records - {financialYearStart:MMM yyyy} to {financialYearStart.AddMonths(11):MMM yyyy} ({(_showAmount ? "Amount" : "Quantity")})";

		_monthlySummaries = [.. _transactionOverviews
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var purchases = g.Where(t => t.Quantity > 0).ToList();

				var summary = new PurchaseItemMonthlySummaryModel
				{
					ItemId = g.Key,
					ItemName = g.First().ItemName,
					ItemCode = g.First().ItemCode,
					ItemCategoryId = g.First().ItemCategoryId,
					ItemCategoryName = g.First().ItemCategoryName,
					UnitOfMeasurement = g.First().UnitOfMeasurement,
					TotalQuantity = g.Sum(t => t.Quantity),
					TotalAmount = g.Sum(t => t.NetTotal),
					DiscountAmount = g.Sum(t => t.DiscountAmount),
					TaxAmount = g.Sum(t => t.TotalTaxAmount),
					ReturnQuantity = -g.Where(t => t.Quantity < 0).Sum(t => t.Quantity),
					ReturnAmount = -g.Where(t => t.NetTotal < 0).Sum(t => t.NetTotal),
					MinimumRate = purchases.Count == 0 ? 0 : purchases.Min(t => t.NetRate),
					MaximumRate = purchases.Count == 0 ? 0 : purchases.Max(t => t.NetRate),
					LastRate = purchases.Count == 0 ? 0 : purchases.OrderBy(t => t.TransactionDateTime).Last().NetRate,
					TransactionCount = g.Select(t => t.MasterId).Distinct().Count(),
					PartyCount = g.Select(t => t.PartyId).Distinct().Count(),
					FirstPurchaseDateTime = g.Min(t => t.TransactionDateTime),
					LastPurchaseDateTime = g.Max(t => t.TransactionDateTime)
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
			summary.MonthsSinceLastPurchase = summary.LastPurchaseDateTime is null
				? 0
				: ((_currentDateTime.Year - summary.LastPurchaseDateTime.Value.Year) * 12) + _currentDateTime.Month - summary.LastPurchaseDateTime.Value.Month;
		}
	}

	private static int FindMonthIndex(PurchaseItemMonthlySummaryModel summary, decimal value)
	{
		for (var index = 0; index < 12; index++)
			if (summary[index] == value)
				return index;

		return 0;
	}

	private void MergeTransactionAndReturns() =>
		_transactionOverviews.AddRange(_allTransactionReturnOverviews.Select(pr => new PurchaseItemOverviewModel
		{
			Id = pr.Id,
			MasterId = pr.MasterId,
			ItemId = pr.ItemId,
			ItemName = pr.ItemName,
			ItemCode = pr.ItemCode,
			ItemCategoryId = pr.ItemCategoryId,
			ItemCategoryName = pr.ItemCategoryName,
			Quantity = -pr.Quantity,
			UnitOfMeasurement = pr.UnitOfMeasurement,
			Rate = pr.Rate,
			ItemBaseTotal = -pr.ItemBaseTotal,
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
			NetTotal = -pr.NetTotal,
			NetRate = pr.NetRate,
			ItemRemarks = pr.ItemRemarks,

			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = pr.PartyId,
			PartyName = pr.PartyName,
			TransactionDateTime = pr.TransactionDateTime,
			CashDiscountAmount = -pr.CashDiscountAmount,
			OtherChargesAmount = -pr.OtherChargesAmount,
			RoundOffAmount = -pr.RoundOffAmount,
			TotalAmount = -pr.TotalAmount,
			BaseTotal = -pr.BaseTotal,
			CashDiscountPercent = pr.CashDiscountPercent,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DocumentUrl = pr.DocumentUrl,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			Remarks = pr.Remarks,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
			ItemDiscountAmount = -pr.ItemDiscountAmount,
			TotalAfterItemDiscount = -pr.TotalAfterItemDiscount,
			TotalExtraTaxAmount = -pr.TotalExtraTaxAmount,
			TotalInclusiveTaxAmount = -pr.TotalInclusiveTaxAmount,
			TotalAfterTax = -pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			TransactionNo = pr.TransactionNo,
			ChallanNo = pr.ChallanNo,
			OtherChargesPercent = pr.OtherChargesPercent,

			FinancialAccountingId = pr.FinancialAccountingId,
			FinancialAccountingTransactionNo = pr.FinancialAccountingTransactionNo,

			MasterStatus = pr.MasterStatus
		}));
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

			var (stream, fileName) = PurchaseItemMonthlyReportExport.ExportReport(
				_monthlySummaries,
				_monthHeaders,
				_currentDateTime,
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showAmount,
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
