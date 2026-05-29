using Syncfusion.Blazor.Grids;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.FinancialAccounting.Data;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Exports;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.User.Data;
using PrimeBakesLibrary.Operations.Settings.Data;
using PrimeBakesLibrary.Operations.User.Models;
using PrimeBakesLibrary.Operations.Settings.Models;
using PrimeBakesLibrary.Utils.ExportUtils;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class TrialBalancePage : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel? _selectedCompany = null;
	private GroupModel? _selectedGroup = null;
	private AccountTypeModel? _selectedAccountType = null;

	private List<CompanyModel> _companies = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<TrialBalanceModel> _trialBalance = [];

	private SfGrid<TrialBalanceModel> _sfGrid;
	private CustomDateRangePicker _sfFirstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts, UserRoles.Reports]);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await LoadTrialBalance();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;

		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_groups = await CommonData.LoadTableDataByStatus<GroupModel>(AccountNames.Group);
		_accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(AccountNames.AccountType);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_groups = [.. _groups.OrderBy(s => s.Name)];
		_accountTypes = [.. _accountTypes.OrderBy(s => s.Name)];
	}

	private async Task LoadTrialBalance()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_trialBalance = await FinancialAccountingData.LoadTrialBalanceByCompanyDate(
				_selectedCompany?.Id ?? 0,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			if (_selectedGroup?.Id > 0)
				_trialBalance = [.. _trialBalance.Where(_ => _.GroupId == _selectedGroup.Id)];

			if (_selectedAccountType?.Id > 0)
				_trialBalance = [.. _trialBalance.Where(_ => _.AccountTypeId == _selectedAccountType.Id)];

			_trialBalance = [.. _trialBalance.OrderBy(_ => _.LedgerName)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
		}
		finally
		{
			if (_sfGrid is not null)
				await _sfGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await LoadTrialBalance();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		_selectedCompany = value;
		await LoadTrialBalance();
	}

	private async Task OnAccountTypeChanged(AccountTypeModel value)
	{
		_selectedAccountType = value;
		await LoadTrialBalance();
	}

	private async Task OnGroupChanged(GroupModel value)
	{
		_selectedGroup = value;
		await LoadTrialBalance();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadTrialBalance();
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await TrialBalanceReportExport.ExportReport(
					_trialBalance,
					ReportExportType.Excel,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_selectedCompany?.Id > 0 ? _selectedCompany : null,
					_selectedGroup?.Id > 0 ? _selectedGroup : null,
					_selectedAccountType?.Id > 0 ? _selectedAccountType : null
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

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = await TrialBalanceReportExport.ExportReport(
					_trialBalance,
					ReportExportType.PDF,
					DateOnly.FromDateTime(_fromDate),
				 DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_selectedCompany?.Id > 0 ? _selectedCompany : null,
					_selectedGroup?.Id > 0 ? _selectedGroup : null,
					_selectedAccountType?.Id > 0 ? _selectedAccountType : null
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
	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction": await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccounting, FormFactor, JSRuntime, NavigationManager); break;
			case "Refresh": await LoadTrialBalance(); break;
			case "ToggleDetailsView": await ToggleDetailsView(); break;
			case "ExportPdf": await ExportPdf(); break;
			case "ExportExcel": await ExportExcel(); break;
			case "TransactionHistory": await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager); break;
			case "LedgerReport": await AuthenticationService.NavigateToRoute(PageRouteNames.AccountingLedgerReport, FormFactor, JSRuntime, NavigationManager); break;
			case "ProfitLoss": await AuthenticationService.NavigateToRoute(PageRouteNames.ProfitAndLossReport, FormFactor, JSRuntime, NavigationManager); break;
			case "BalanceSheet": await AuthenticationService.NavigateToRoute(PageRouteNames.BalanceSheetReport, FormFactor, JSRuntime, NavigationManager); break;
			case "PeriodToday": await HandleDatesChanged(DateRangeType.Today); break;
			case "PeriodPreviousDay": await HandleDatesChanged(DateRangeType.Yesterday); break;
			case "PeriodNextDay": await HandleDatesChanged(DateRangeType.NextDay); break;
			case "PeriodCurrentMonth": await HandleDatesChanged(DateRangeType.CurrentMonth); break;
			case "PeriodPreviousMonth": await HandleDatesChanged(DateRangeType.PreviousMonth); break;
			case "PeriodNextMonth": await HandleDatesChanged(DateRangeType.NextMonth); break;
			case "PeriodCurrentFinancialYear": await HandleDatesChanged(DateRangeType.CurrentFinancialYear); break;
			case "PeriodPreviousFinancialYear": await HandleDatesChanged(DateRangeType.PreviousFinancialYear); break;
			case "PeriodNextFinancialYear": await HandleDatesChanged(DateRangeType.NextFinancialYear); break;
			case "PeriodAllTime": await HandleDatesChanged(DateRangeType.AllTime); break;
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
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
				await LoadTrialBalance();
		}
		catch (OperationCanceledException)
		{
			// Timer was cancelled, expected on dispose
		}
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
