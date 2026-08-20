using System.Globalization;

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Attendance;

namespace PrimeBakes.Exports.Payroll.Attendance;

public static class AttendanceExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<AttendanceOverviewModel> attendanceData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = attendanceData.Select(attendance => new
		{
			attendance.Id,
			attendance.EmployeeCode,
			attendance.EmployeeName,
			Period = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(attendance.AttendanceMonth)} {attendance.AttendanceYear}",
			attendance.DaysInMonth,
			attendance.PresentDays,
			attendance.WeeklyOffDays,
			attendance.HolidayDays,
			attendance.PaidLeaveDays,
			attendance.UnpaidLeaveDays,
			attendance.PaidDays,
			attendance.OvertimeHours,
			attendance.Remarks,
			Status = attendance.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(AttendanceOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(AttendanceOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(AttendanceOverviewModel.EmployeeName)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			["Period"] = new() { DisplayName = "Period", Alignment = CellAlignment.Center },
			[nameof(AttendanceOverviewModel.DaysInMonth)] = new() { DisplayName = "Days In Month", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(AttendanceOverviewModel.PresentDays)] = new() { DisplayName = "Present", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.WeeklyOffDays)] = new() { DisplayName = "Weekly Off", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.HolidayDays)] = new() { DisplayName = "Holiday", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.PaidLeaveDays)] = new() { DisplayName = "Paid Leave", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.UnpaidLeaveDays)] = new() { DisplayName = "Unpaid Leave", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.PaidDays)] = new() { DisplayName = "Paid Days", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.OvertimeHours)] = new() { DisplayName = "OT Hours", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(AttendanceOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(AttendanceOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center }
		};

		List<string> columnOrder =
		[
			nameof(AttendanceOverviewModel.Id),
			nameof(AttendanceOverviewModel.EmployeeCode),
			nameof(AttendanceOverviewModel.EmployeeName),
			"Period",
			nameof(AttendanceOverviewModel.DaysInMonth),
			nameof(AttendanceOverviewModel.PresentDays),
			nameof(AttendanceOverviewModel.WeeklyOffDays),
			nameof(AttendanceOverviewModel.HolidayDays),
			nameof(AttendanceOverviewModel.PaidLeaveDays),
			nameof(AttendanceOverviewModel.UnpaidLeaveDays),
			nameof(AttendanceOverviewModel.PaidDays),
			nameof(AttendanceOverviewModel.OvertimeHours),
			nameof(AttendanceOverviewModel.Remarks),
			nameof(AttendanceOverviewModel.Status)
		];

		var fileName = $"Attendance_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"ATTENDANCE MASTER",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"ATTENDANCE",
				"Attendance Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
