using PrimeBakes.Data.Payroll.PayrollRun;
using PrimeBakes.Exports.Payroll.PayrollRun;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.PayrollRun;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Globalization;

namespace PrimeBakes.Shared.Pages.Payroll.PayrollRun;

public partial class PayrollPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private int _payrollMonth;
	private int _payrollYear;
	private string _selectedMonthName;

	private List<PayrollOverviewModel> _payrollOverviews = [];
	private List<PayrollItemOverviewModel> _payrollItems = [];
	private PayrollOverviewModel _selectedPayroll;

	private static readonly List<string> _monthNames =
		[.. Enumerable.Range(1, 12).Select(month => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month))];

	private readonly List<ContextMenuItemModel> _gridContextMenuItems =
	[
		new() { Text = "Payslip PDF (Alt + P)", Id = "PayslipPDF", IconCss = "e-icons e-export-pdf", Target = ".e-content" },
		new() { Text = "Payslip Excel (Alt + E)", Id = "PayslipExcel", IconCss = "e-icons e-export-excel", Target = ".e-content" },
		new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfGrid<PayrollOverviewModel> _sfGrid;
	private SfGrid<PayrollItemOverviewModel> _sfItemGrid;
	private CustomAutoComplete<string> _firstFocus;
	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	private decimal _totalNetPay;

	private static string PeriodText(int month, int year) =>
		month is < 1 or > 12 ? string.Empty : $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}";

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Payroll], true);
			await LoadData();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		_payrollMonth = currentDateTime.Month;
		_payrollYear = currentDateTime.Year;
		_selectedMonthName = _monthNames[currentDateTime.Month - 1];

		await LoadOverviews();

		_isLoading = false;
		StateHasChanged();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadOverviews()
	{
		_payrollOverviews = await PayrollData.LoadPayrollOverviewByEmployeeMonthYear(
			PayrollMonth: _payrollMonth, PayrollYear: _payrollYear);

		if (!_showDeleted)
			_payrollOverviews = [.. _payrollOverviews.Where(p => p.Status)];

		_payrollOverviews = [.. _payrollOverviews.OrderBy(p => p.EmployeeCode)];
		_totalNetPay = _payrollOverviews.Where(p => p.Status).Sum(p => p.NetPay);

		_selectedPayroll = null;
		_payrollItems = [];

		if (_sfGrid is not null) await _sfGrid.Refresh();
	}
	#endregion

	#region Changed Events
	private async Task OnPeriodChanged()
	{
		_payrollMonth = _monthNames.IndexOf(_selectedMonthName ?? string.Empty) + 1;

		if (_payrollMonth is < 1 or > 12 || _payrollYear is < 2000 or > 2100)
			return;

		await LoadOverviews();
		StateHasChanged();
	}

	private async Task OnRowSelected(RowSelectEventArgs<PayrollOverviewModel> args)
	{
		_selectedPayroll = args.Data;
		_payrollItems = await CommonData.LoadTableDataByMasterId<PayrollItemOverviewModel>(PayrollNames.PayrollItemOverview, args.Data.Id);
		_payrollItems = [.. _payrollItems.OrderBy(i => i.Sequence)];

		StateHasChanged();

		if (_sfItemGrid is not null) await _sfItemGrid.Refresh();
	}
	#endregion

	#region Saving
	private async Task ConfirmRunPayroll()
	{
		if (_isProcessing)
			return;

		_payrollMonth = _monthNames.IndexOf(_selectedMonthName ?? string.Empty) + 1;
		if (_payrollMonth is < 1 or > 12)
		{
			await _toastNotification.ShowAsync("Cannot Run", "Please select a valid month.", ToastType.Warning);
			return;
		}

		var existing = _payrollOverviews.Count(p => p.Status);
		var message = existing > 0
			? $"{existing} payroll record(s) already exist for {PeriodText(_payrollMonth, _payrollYear)}. Running again will recalculate and overwrite them. Continue?"
			: $"Run payroll for every employee with attendance in {PeriodText(_payrollMonth, _payrollYear)}?";

		await ShowConfirmation("Run Payroll", message, RunPayroll);
	}

	private async Task RunPayroll()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			await _toastNotification.ShowAsync("Processing", "Please wait while payroll is being calculated...", ToastType.Info);

			var processed = await PayrollData.RunPayroll(_payrollMonth, _payrollYear, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));

			await LoadOverviews();

			await _toastNotification.ShowAsync("Saved", $"Payroll processed for {processed} employee(s).", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Actions
	private async Task DeleteRecoverTransaction(int id, bool isRecover)
	{
		try
		{
			if (!_user.Admin)
				throw new Exception("You do not have permission to perform this action.");

			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing", $"{(isRecover ? "Recovering" : "Deleting")} transaction...", ToastType.Info);

			var payroll = await CommonData.LoadTableDataById<PayrollModel>(PayrollNames.Payroll, id)
				?? throw new Exception("Transaction not found.");

			if (isRecover) await PayrollData.RecoverTransaction(payroll, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));
			else await PayrollData.DeleteTransaction(payroll, _user.Id, await PlatformInfo.GetCreatedFromPlatform(FormFactor, LocationService));

			await LoadOverviews();

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
		}
	}

	private async Task DeleteRecoverSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count == 0)
			return;

		var record = selectedRecords[0];

		await ShowConfirmation(record.Status ? "Delete" : "Recover",
			$"Are you sure you want to {(record.Status ? "delete" : "recover")} payroll for {record.EmployeeName} - {PeriodText(record.PayrollMonth, record.PayrollYear)}",
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

	private async Task ExportMaster(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var (stream, fileName) = PayrollExport.ExportMaster(_payrollOverviews, await CommonData.LoadCurrentDateTime(), isExcel ? ReportExportType.Excel : ReportExportType.PDF);
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

	private async Task ExportItems(bool isExcel = false)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (_payrollOverviews.Count == 0)
			{
				await _toastNotification.ShowAsync("Cannot View", "There is no payroll to export for this period.", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			List<PayrollItemOverviewModel> items = [];
			foreach (var payroll in _payrollOverviews)
				items.AddRange(await CommonData.LoadTableDataByMasterId<PayrollItemOverviewModel>(PayrollNames.PayrollItemOverview, payroll.Id));

			var (stream, fileName) = PayrollExport.ExportItems(
				items.OrderBy(i => i.EmployeeCode).ThenBy(i => i.Sequence),
				await CommonData.LoadCurrentDateTime(),
				isExcel ? ReportExportType.Excel : ReportExportType.PDF);

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
		switch (args.Item.Id)
		{
			case "PayslipPDF": await ExportSelectedPayslip(); break;
			case "PayslipExcel": await ExportSelectedPayslip(true); break;
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
