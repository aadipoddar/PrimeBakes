using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Payroll.PayrollRun;
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

public partial class PayrollReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showDeleted = false;
	private double _locationRadius = 1000;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private EmployeeModel? _selectedEmployee = null;
	private DepartmentModel? _selectedDepartment = null;
	private DesignationModel? _selectedDesignation = null;

	private List<EmployeeModel> _employees = [];
	private List<DepartmentModel> _departments = [];
	private List<DesignationModel> _designations = [];
	private List<PayrollOverviewModel> _transactionOverviews = [];
	private List<PayrollOverviewModel> _allTransactionOverviews = [];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "View (Alt + O)", Id = "View", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Payslip PDF (Alt + P)", Id = "PayslipPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Payslip Excel (Alt + E)", Id = "PayslipExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecover", IconCss = "e-icons e-trash", Target = ".e-content" },
		new() { Text = "Open Created Location", Id = "OpenCreatedLocation", IconCss = "e-icons e-location", Target = ".e-content" },
		new() { Text = "Open Modified Location", Id = "OpenModifiedLocation", IconCss = "e-icons e-location", Target = ".e-content" }
	];

	private SfGrid<PayrollOverviewModel> _sfGrid;
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
			_user = await AuthService.ValidateUser([UserRoles.Payroll, UserRoles.Reports], true);
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
		var currentDateTimeTask = CommonData.LoadCurrentDateTime();
		var employees = CommonData.LoadTableDataByStatus<EmployeeModel>(PayrollNames.Employee);
		var departments = CommonData.LoadTableDataByStatus<DepartmentModel>(PayrollNames.Department);
		var designations = CommonData.LoadTableDataByStatus<DesignationModel>(PayrollNames.Designation);

		var currentDateTime = await currentDateTimeTask;
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(DateRangeType.CurrentMonth, currentDateTime, currentDateTime);
		_employees = [.. (await employees).OrderBy(e => e.Name)];
		_departments = [.. (await departments).OrderBy(d => d.Name)];
		_designations = [.. (await designations).OrderBy(d => d.Name)];
	}

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching payroll...", ToastType.Info);

			_allTransactionOverviews = await CommonData.LoadTableDataByDate<PayrollOverviewModel>(
				PayrollNames.PayrollOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			await ApplyFilters();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load payroll: {ex.Message}", ToastType.Error);
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

		if (!_showDeleted)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.Status)];

		if (_selectedEmployee?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.EmployeeId == _selectedEmployee.Id)];

		if (_selectedDepartment?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.DepartmentId == _selectedDepartment.Id)];

		if (_selectedDesignation?.Id > 0)
			_transactionOverviews = [.. _transactionOverviews.Where(t => t.DesignationId == _selectedDesignation.Id)];

		_transactionOverviews = [.. _transactionOverviews.OrderBy(t => t.TransactionDateTime).ThenBy(t => t.EmployeeCode)];

		if (_showSummary)
			_transactionOverviews = [.. _transactionOverviews
				.GroupBy(t => t.DepartmentName)
				.Select(g => new PayrollOverviewModel
				{
					DepartmentName = g.Key,
					PaidDays = g.Sum(t => t.PaidDays),
					GrossEarnings = g.Sum(t => t.GrossEarnings),
					TotalDeductions = g.Sum(t => t.TotalDeductions),
					NetPay = g.Sum(t => t.NetPay),
					EmployerContribution = g.Sum(t => t.EmployerContribution)
				})
				.OrderBy(t => t.DepartmentName)];

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

	private async Task OnDesignationChanged(DesignationModel value)
	{
		_selectedDesignation = value;
		await ApplyFilters();
	}
	#endregion

	#region Actions
	private async Task ViewSelectedTransaction()
	{
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		await WindowNavigation.NavigateToRoute(PayrollRouteNames.Payroll);
	}

	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		if (_isProcessing || id == 0)
			return;

		try
		{
			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission for the action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var payroll = await CommonData.LoadTableDataById<PayrollModel>(PayrollNames.Payroll, id)
				?? throw new Exception("Transaction not found.");

			var platform = await PlatformInfo.GetPlatformInfo();

			if (isRecover) await PayrollData.RecoverTransaction(payroll, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);
			else await PayrollData.DeleteTransaction(payroll, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Success", $"Transaction has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
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
		if (_isProcessing || _showSummary || _sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} payroll {record.TransactionNo} for {record.EmployeeName}",
			() => DeleteRecoverTransaction(record.Id, !record.Status));
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
	private async Task ExportComponentReport(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (_transactionOverviews.Count == 0)
			{
				await _toastNotification.ShowAsync("Cannot Export", "There is no payroll in the current filter.", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var components = await CommonData.LoadTableDataByDate<PayrollItemOverviewModel>(
				PayrollNames.PayrollItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			var platform = await PlatformInfo.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = PayrollRouteNames.PayrollReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFormFactor = platform.FormFactor,
				CreatedPlatform = platform.Platform,
				CreatedLatitude = platform.Latitude,
				CreatedLongitude = platform.Longitude
			});

			var shown = _transactionOverviews.Select(t => t.Id).ToHashSet();
			components = [.. components
				.Where(c => shown.Contains(c.MasterId) && c.SalaryComponentType != SalaryComponentTypes.Info.ToString())
				.OrderBy(c => c.EmployeeCode)
				.ThenBy(c => c.Sequence)];

			var (stream, fileName) = PayrollReportExport.ExportItemReport(
				components,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
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

			var (stream, fileName) = PayrollReportExport.ExportReport(
				_transactionOverviews,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns,
				_showDeleted,
				_showSummary,
				_selectedEmployee?.Id > 0 ? _selectedEmployee : null,
				_selectedDepartment?.Id > 0 ? _selectedDepartment : null,
				_selectedDesignation?.Id > 0 ? _selectedDesignation : null
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<PayrollOverviewModel> args)
	{
		if (_showSummary)
			return;

		switch (args.Item.Id)
		{
			case "View": await ViewSelectedTransaction(); break;
			case "PayslipPDF": await ExportSelectedPayslip(); break;
			case "PayslipExcel": await ExportSelectedPayslip(true); break;
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
	}
	#endregion
}
