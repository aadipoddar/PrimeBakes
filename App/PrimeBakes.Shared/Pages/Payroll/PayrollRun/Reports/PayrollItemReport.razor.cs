using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Exports.Payroll.PayrollRun;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Models.Payroll.PayrollRun;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Payroll.PayrollRun.Reports;

public partial class PayrollItemReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showInfoComponents = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private EmployeeModel? _selectedEmployee = null;
	private DepartmentModel? _selectedDepartment = null;
	private SalaryComponentModel? _selectedSalaryComponent = null;

	private List<EmployeeModel> _employees = [];
	private List<DepartmentModel> _departments = [];
	private List<SalaryComponentModel> _salaryComponents = [];
	private List<PayrollItemOverviewModel> _transactionOverviews = [];
	private List<PayrollItemOverviewModel> _allTransactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Payslip PDF (Alt + P)", Id = "PayslipPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Payslip Excel (Alt + E)", Id = "PayslipExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" }
	];

	private SfGrid<PayrollItemOverviewModel> _sfGrid;
	private CustomDateRangePicker _firstFocus;
	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Payroll, UserRoles.Reports], true);
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
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(DateRangeType.CurrentMonth, currentDateTime, currentDateTime);

		_employees = await CommonData.LoadTableDataByStatus<EmployeeModel>(PayrollNames.Employee);
		_departments = await CommonData.LoadTableDataByStatus<DepartmentModel>(PayrollNames.Department);
		_salaryComponents = await CommonData.LoadTableDataByStatus<SalaryComponentModel>(PayrollNames.SalaryComponent);

		_employees = [.. _employees.OrderBy(e => e.Name)];
		_departments = [.. _departments.OrderBy(d => d.Name)];
		_salaryComponents = [.. _salaryComponents.OrderBy(s => s.Sequence)];
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching payroll components...", ToastType.Info);

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<PayrollItemOverviewModel>(
				PayrollNames.PayrollItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			await ApplyFilters();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load payroll components: {ex.Message}", ToastType.Error);
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
		_transactionOverviews = [.. _allTransactionOverviews.Where(t => t.MasterStatus)];

		if (!_showInfoComponents)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.SalaryComponentType != SalaryComponentTypes.Info.ToString())];

		if (_selectedEmployee?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.EmployeeId == _selectedEmployee.Id)];

		if (_selectedDepartment?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.DepartmentId == _selectedDepartment.Id)];

		if (_selectedSalaryComponent?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.SalaryComponentId == _selectedSalaryComponent.Id)];

		_transactionOverviews = [.. _transactionOverviews
			.OrderBy(t => t.TransactionDateTime)
			.ThenBy(t => t.EmployeeCode)
			.ThenBy(t => t.Sequence)];

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => new { t.SalaryComponentId, t.SalaryComponentCode, t.SalaryComponentName, t.SalaryComponentType, t.Sequence })
				.Select(g => new PayrollItemOverviewModel
				{
					SalaryComponentId = g.Key.SalaryComponentId,
					SalaryComponentCode = g.Key.SalaryComponentCode,
					SalaryComponentName = g.Key.SalaryComponentName,
					SalaryComponentType = g.Key.SalaryComponentType,
					Sequence = g.Key.Sequence,
					Amount = g.Sum(t => t.Amount)
				})
				.OrderBy(t => t.Sequence)];

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

	private async Task OnEmployeeChanged(EmployeeModel value)
	{
		_selectedEmployee = value;
		await ApplyFilters();
	}

	private async Task OnDepartmentChanged(DepartmentModel value)
	{
		_selectedDepartment = value;
		await ApplyFilters();
	}

	private async Task OnSalaryComponentChanged(SalaryComponentModel value)
	{
		_selectedSalaryComponent = value;
		await ApplyFilters();
	}
	#endregion

	#region Exporting
	private async Task ExportPayrollReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var payrolls = await CommonData.LoadTableDataByDate<PayrollOverviewModel>(
				PayrollNames.PayrollOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = PayrollRouteNames.PayrollItemReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform()
			});

			payrolls = [.. payrolls.Where(p => p.Status)];

			if (_selectedEmployee?.Id > 0)
				payrolls = [.. payrolls.Where(p => p.EmployeeId == _selectedEmployee.Id)];

			if (_selectedDepartment?.Id > 0)
				payrolls = [.. payrolls.Where(p => p.DepartmentId == _selectedDepartment.Id)];

			if (payrolls.Count == 0)
			{
				await _toastNotification.ShowAsync("Cannot Export", "There is no payroll in the current filter.", ToastType.Warning);
				return;
			}

			payrolls = [.. payrolls.OrderBy(p => p.TransactionDateTime).ThenBy(p => p.EmployeeCode)];

			var (stream, fileName) = PayrollReportExport.ExportReport(
				payrolls,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				false,
				false,
				_selectedEmployee?.Id > 0 ? _selectedEmployee : null,
				_selectedDepartment?.Id > 0 ? _selectedDepartment : null,
				null
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

	private async Task ExportSelectedPayslip(bool isExcel = false)
	{
		if (_isProcessing || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		if (_showSummary)
		{
			await _toastNotification.ShowAsync("Cannot Export", "Turn off Summary to export a payslip for a single employee.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Payslip...", ToastType.Info);

			var decodedTransactionNo = await DecodeCode.DecodeTransactionNo(_sfGrid.SelectedRecords.First().TransactionNo, !isExcel, isExcel);
			await SaveAndViewService.SaveAndView(
				isExcel ? decodedTransactionNo.ExcelStream.fileName : decodedTransactionNo.PDFStream.fileName,
				isExcel ? decodedTransactionNo.ExcelStream.stream : decodedTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The payslip has been downloaded successfully.", ToastType.Success);
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

	private async Task ExportReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = PayrollReportExport.ExportItemReport(
				_transactionOverviews,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showSummary,
				_selectedEmployee?.Id > 0 ? _selectedEmployee : null,
				_selectedDepartment?.Id > 0 ? _selectedDepartment : null,
				_selectedSalaryComponent?.Id > 0 ? _selectedSalaryComponent : null
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<PayrollItemOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "PayslipPDF": await ExportSelectedPayslip(); break;
			case "PayslipExcel": await ExportSelectedPayslip(true); break;
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}

	private async Task ToggleInfoComponents()
	{
		_showInfoComponents = !_showInfoComponents;
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
	}
	#endregion
}
