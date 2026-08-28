using Microsoft.AspNetCore.Components;
using PrimeBakes.Exports.Operations.AuditTrail;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Operations.AuditTrail;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class AuditTrailReport : IAsyncDisposable
{
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private double _locationRadius = 1000;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Open Changes (Alt + O)", Id = "OpenChanges", IconCss = "e-icons e-eye", Target = ".e-content" },
		new() { Text = "Open Location", Id = "OpenLocation", IconCss = "e-icons e-location", Target = ".e-content" }
	];

	private List<AuditTrailOverviewModel> _auditTrails = [];

	private SfGrid<AuditTrailOverviewModel> _sfGrid;
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
			_user = await AuthService.ValidateUser([UserRoles.Admin], true);
			await InitializePage();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task InitializePage()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;

		await LoadAuditTrails();
		await StartAutoRefresh();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadAuditTrails()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching audit trail records...", ToastType.Info);

			_auditTrails = await CommonData.LoadTableDataByDate<AuditTrailOverviewModel>(
				OperationNames.AuditTrailOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MinValue));

			var platform = await AuthService.GetPlatformInfo();
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Report.ToString(),
				TableName = OperationRouteNames.AuditTrailReport,
				RecordNo = $"{_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
				CreatedBy = _user.Id,
				CreatedFormFactor = platform.FormFactor,
				CreatedPlatform = platform.Platform,
				CreatedLatitude = platform.Latitude,
				CreatedLongitude = platform.Longitude
			});

			_auditTrails = [.. _auditTrails.OrderByDescending(_ => _.TransactionDateTime)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load audit trail: {ex.Message}", ToastType.Error);
		}
		finally
		{
			if (_sfGrid is not null)
				await _sfGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(MudBlazor.DateRange range)
	{
		_fromDate = range?.Start ?? _fromDate;
		_toDate = range?.End ?? _toDate;
		await LoadAuditTrails();
	}

	private async Task HandleDatesChanged(DateRangeType dateRangeType)
	{
		(_fromDate, _toDate) = await FinancialYearData.GetDateRange(dateRangeType, _fromDate, _toDate);
		await LoadAuditTrails();
	}
	#endregion

	#region Actions
	private async Task OpenChanges()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var record = _sfGrid.SelectedRecords.First();

		var sb = new StringBuilder();
		sb.AppendLine("AUDIT TRAIL RECORD");
		sb.AppendLine(new string('=', 60));
		sb.AppendLine($"Module   : {record.TableName}");
		sb.AppendLine($"Record   : {record.RecordNo}");
		sb.AppendLine($"Action   : {record.Action}");
		sb.AppendLine($"Date     : {record.TransactionDateTime:dd-MMM-yyyy HH:mm}");
		sb.AppendLine($"User     : {record.CreatedByName}");
		sb.AppendLine($"Form     : {record.CreatedFormFactor}");
		sb.AppendLine($"Platform : {record.CreatedPlatform}");

		if (record.CreatedLatitude is not null && record.CreatedLongitude is not null)
			sb.AppendLine($"Location : {record.CreatedLatitude:F6}, {record.CreatedLongitude:F6}");

		if (!string.IsNullOrWhiteSpace(record.RecordValue))
		{
			sb.AppendLine();
			sb.AppendLine("CHANGES");
			sb.AppendLine(new string('-', 60));
			sb.AppendLine(record.RecordValue);
		}
		else
		{
			sb.AppendLine();
			sb.AppendLine("CHANGES");
			sb.AppendLine(new string('-', 60));
			sb.AppendLine("(no changes recorded)");
		}

		var bytes = Encoding.UTF8.GetBytes(sb.ToString());
		var stream = new MemoryStream(bytes);
		await SaveAndViewService.SaveAndView($"AuditTrail_{record.TableName}_{record.RecordNo}.txt", stream);
	}

	private async Task DeleteRecords()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Deleting audit trail records...", ToastType.Info);

			var platform = await AuthService.GetPlatformInfo();
			var deleted = await AuditTrailData.DeleteAuditTrailByDate(_fromDate, _toDate, _user.Id,
				platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Deleted", $"{deleted} audit trail records have been deleted successfully.", ToastType.Success);
			await LoadAuditTrails();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete audit trail records: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await _toastNotification.HideAllInfoAsync();
		}
	}

	private async Task DeleteRecordsSelectedRange() =>
		await ShowConfirmation("Delete Records",
			$"Are you sure you want to permanently delete every audit trail record from {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}",
			DeleteRecords);

	private async Task OpenSelectedRecordLocation()
	{
		var record = _sfGrid?.SelectedRecords?.FirstOrDefault();
		await LocationService.OpenMapAsync(record?.CreatedLatitude, record?.CreatedLongitude);
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

			var (stream, fileName) = AuditTrailExport.ExportReport(
				_auditTrails,
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF,
				DateOnly.FromDateTime(_fromDate),
				DateOnly.FromDateTime(_toDate),
				_showAllColumns
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<AuditTrailOverviewModel> args)
	{
		if (args.Item.Id == "OpenChanges") await OpenChanges();
		if (args.Item.Id == "OpenLocation") await OpenSelectedRecordLocation();
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null) await _sfGrid.Refresh();
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
				await LoadAuditTrails();
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
