using PrimeBakes.Data.Payroll.Attendance;
using PrimeBakes.Exports.Payroll.Attendance;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Attendance;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Globalization;

namespace PrimeBakes.Shared.Pages.Payroll.Attendance;

public partial class AttendancePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private AttendanceModel _attendance = new();
	private EmployeeModel _selectedEmployee;
	private string _selectedMonthName;

	private List<EmployeeModel> _employees = [];
	private List<AttendanceOverviewModel> _attendanceOverviews = [];

	private static readonly List<string> _monthNames =
		[.. Enumerable.Range(1, 12).Select(month => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month))];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<AttendanceOverviewModel> _sfGrid;
	private CustomAutoComplete<EmployeeModel> _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	private decimal PaidDays =>
		_attendance.PresentDays + _attendance.WeeklyOffDays + _attendance.HolidayDays + _attendance.PaidLeaveDays;

	private static string PeriodText(int month, int year) =>
		month is < 1 or > 12 ? string.Empty : $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}";

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthService.ValidateUser([UserRoles.Payroll], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		_employees = await CommonData.LoadTableDataByStatus<EmployeeModel>(PayrollNames.Employee);
		_employees = [.. _employees.OrderBy(e => e.Name)];

		_attendance.AttendanceMonth = currentDateTime.Month;
		_attendance.AttendanceYear = currentDateTime.Year;
		_selectedMonthName = _monthNames[currentDateTime.Month - 1];
		_attendance.DaysInMonth = DateTime.DaysInMonth(_attendance.AttendanceYear, _attendance.AttendanceMonth);

		await LoadOverviews();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadOverviews()
	{
		_attendanceOverviews = await AttendanceData.LoadAttendanceOverviewByEmployeeMonthYear(
			AttendanceMonth: _attendance.AttendanceMonth, AttendanceYear: _attendance.AttendanceYear);

		if (!_showDeleted)
			_attendanceOverviews = [.. _attendanceOverviews.Where(a => a.Status)];

		_attendanceOverviews = [.. _attendanceOverviews.OrderBy(a => a.EmployeeCode)];

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}
	#endregion

	#region Changed Events
	private async Task OnPeriodChanged()
	{
		_attendance.EmployeeId = _selectedEmployee?.Id ?? 0;
		_attendance.AttendanceMonth = _monthNames.IndexOf(_selectedMonthName ?? string.Empty) + 1;

		if (_attendance.AttendanceMonth is >= 1 and <= 12 && _attendance.AttendanceYear is >= 2000 and <= 2100)
			_attendance.DaysInMonth = DateTime.DaysInMonth(_attendance.AttendanceYear, _attendance.AttendanceMonth);
		else
			_attendance.DaysInMonth = 0;

		await LoadOverviews();
		await LoadExistingPeriod();

		StateHasChanged();
	}

	// The month is unique per employee, so selecting a period that already has a row
	// loads it for editing instead of failing the duplicate check on save.
	private async Task LoadExistingPeriod()
	{
		if (_attendance.EmployeeId <= 0 || _attendance.DaysInMonth <= 0)
			return;

		var existing = _attendanceOverviews.FirstOrDefault(a => a.EmployeeId == _attendance.EmployeeId);
		if (existing is null)
		{
			ResetDays();
			return;
		}

		var attendance = await CommonData.LoadTableDataById<AttendanceModel>(PayrollNames.Attendance, existing.Id);
		if (attendance is null)
		{
			ResetDays();
			return;
		}

		_attendance = attendance;
	}

	private void ResetDays()
	{
		_attendance.Id = 0;
		_attendance.PresentDays = 0;
		_attendance.WeeklyOffDays = 0;
		_attendance.HolidayDays = 0;
		_attendance.PaidLeaveDays = 0;
		_attendance.UnpaidLeaveDays = 0;
		_attendance.PaidDays = 0;
		_attendance.OvertimeHours = 0;
		_attendance.Remarks = null;
	}
	#endregion

	#region Saving
	private async Task SaveTransaction()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while the transaction is being saved...", ToastType.Info);

			_attendance.EmployeeId = _selectedEmployee?.Id ?? 0;
			_attendance.AttendanceMonth = _monthNames.IndexOf(_selectedMonthName ?? string.Empty) + 1;
			var platform = await AuthService.GetPlatformInfo();
			await AttendanceData.SaveTransaction(_attendance, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Saved", "Transaction has been saved successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Actions
	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		_attendance = await CommonData.LoadTableDataById<AttendanceModel>(PayrollNames.Attendance, selectedRecords[0].Id);
		if (_attendance is null)
		{
			await _toastNotification.ShowAsync("Error while Editing", "Transaction Not Found.", ToastType.Error);
			return;
		}

		_selectedEmployee = _employees.FirstOrDefault(e => e.Id == _attendance.EmployeeId);
		_selectedMonthName = _monthNames[_attendance.AttendanceMonth - 1];

		StateHasChanged();
		await _firstFocus.FocusAsync();
	}

	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var attendance = await CommonData.LoadTableDataById<AttendanceModel>(PayrollNames.Attendance, id)
				?? throw new Exception("Transaction not found.");

			var platform = await AuthService.GetPlatformInfo();
			if (isRecover) await AttendanceData.RecoverTransaction(attendance, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);
			else await AttendanceData.DeleteTransaction(attendance, _user.Id, platform.FormFactor, platform.Platform, platform.Latitude, platform.Longitude);

			await _toastNotification.ShowAsync("Success", $"Transaction has been {(isRecover ? "recovered" : "deleted")} successfully.", ToastType.Success);
			ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while {(isRecover ? "recovering" : "deleting")} transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} attendance for {record.EmployeeName} - {PeriodText(record.AttendanceMonth, record.AttendanceYear)}",
			() => DeleteRecoverTransaction(record.Id, !record.Status));
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
	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = AttendanceExport.ExportMaster(_attendanceOverviews, await CommonData.LoadCurrentDateTime(), isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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
	private async Task OnGridContextMenuItemClicked(ContextMenuClickEventArgs<AttendanceOverviewModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditSelectedItem": await EditSelectedItem(); break;
			case "DeleteRecoverSelectedItem": await DeleteRecoverSelectedItem(); break;
		}
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadOverviews();
		StateHasChanged();
	}

	private void ResetPage() => PageRefresh.Request();
	#endregion
}
