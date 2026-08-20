using System.Globalization;

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.PayrollRun;

namespace PrimeBakes.Exports.Payroll.PayrollRun;

public static class PayrollExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<PayrollOverviewModel> payrollData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = payrollData.Select(payroll => new
		{
			payroll.Id,
			payroll.TransactionNo,
			payroll.EmployeeCode,
			payroll.EmployeeName,
			payroll.DepartmentName,
			payroll.DesignationName,
			Period = Period(payroll.PayrollMonth, payroll.PayrollYear),
			payroll.DaysInMonth,
			payroll.PaidDays,
			payroll.GrossEarnings,
			payroll.TotalDeductions,
			payroll.NetPay,
			payroll.EmployerContribution,
			payroll.Remarks,
			Status = payroll.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PayrollOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollOverviewModel.EmployeeName)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollOverviewModel.DepartmentName)] = new() { DisplayName = "Department", Alignment = CellAlignment.Left },
			[nameof(PayrollOverviewModel.DesignationName)] = new() { DisplayName = "Designation", Alignment = CellAlignment.Left },
			["Period"] = new() { DisplayName = "Period", Alignment = CellAlignment.Center },
			[nameof(PayrollOverviewModel.DaysInMonth)] = new() { DisplayName = "Days In Month", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.PaidDays)] = new() { DisplayName = "Paid Days", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PayrollOverviewModel.GrossEarnings)] = new() { DisplayName = "Gross Earnings", Alignment = CellAlignment.Right, IncludeInTotal = true, Format = "#,##0.00" },
			[nameof(PayrollOverviewModel.TotalDeductions)] = new() { DisplayName = "Deductions", Alignment = CellAlignment.Right, IncludeInTotal = true, Format = "#,##0.00" },
			[nameof(PayrollOverviewModel.NetPay)] = new() { DisplayName = "Net Pay", Alignment = CellAlignment.Right, IncludeInTotal = true, Format = "#,##0.00" },
			[nameof(PayrollOverviewModel.EmployerContribution)] = new() { DisplayName = "Employer Contribution", Alignment = CellAlignment.Right, IncludeInTotal = true, Format = "#,##0.00" },
			[nameof(PayrollOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(PayrollOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center }
		};

		List<string> columnOrder =
		[
			nameof(PayrollOverviewModel.Id),
			nameof(PayrollOverviewModel.TransactionNo),
			nameof(PayrollOverviewModel.EmployeeCode),
			nameof(PayrollOverviewModel.EmployeeName),
			nameof(PayrollOverviewModel.DepartmentName),
			nameof(PayrollOverviewModel.DesignationName),
			"Period",
			nameof(PayrollOverviewModel.DaysInMonth),
			nameof(PayrollOverviewModel.PaidDays),
			nameof(PayrollOverviewModel.GrossEarnings),
			nameof(PayrollOverviewModel.TotalDeductions),
			nameof(PayrollOverviewModel.NetPay),
			nameof(PayrollOverviewModel.EmployerContribution),
			nameof(PayrollOverviewModel.Remarks),
			nameof(PayrollOverviewModel.Status)
		];

		var fileName = $"Payroll_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PAYROLL",
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
				"PAYROLL",
				"Payroll Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static (MemoryStream stream, string fileName) ExportItems(
		IEnumerable<PayrollItemOverviewModel> payrollItemData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = payrollItemData.Select(item => new
		{
			item.Id,
			item.TransactionNo,
			item.EmployeeCode,
			item.EmployeeName,
			Period = Period(item.PayrollMonth, item.PayrollYear),
			item.SalaryComponentCode,
			item.SalaryComponentName,
			item.SalaryComponentType,
			item.Amount,
			item.Formula,
			Prorate = item.Prorate ? "Yes" : "No",
			item.PaidDays,
			item.NetPay
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PayrollItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollItemOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollItemOverviewModel.EmployeeName)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			["Period"] = new() { DisplayName = "Period", Alignment = CellAlignment.Center },
			[nameof(PayrollItemOverviewModel.SalaryComponentCode)] = new() { DisplayName = "Component Code", Alignment = CellAlignment.Left },
			[nameof(PayrollItemOverviewModel.SalaryComponentName)] = new() { DisplayName = "Component", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(PayrollItemOverviewModel.SalaryComponentType)] = new() { DisplayName = "Type", Alignment = CellAlignment.Left },
			[nameof(PayrollItemOverviewModel.Amount)] = new() { DisplayName = "Amount", Alignment = CellAlignment.Right, IncludeInTotal = true, Format = "#,##0.00" },
			[nameof(PayrollItemOverviewModel.Formula)] = new() { DisplayName = "Formula", Alignment = CellAlignment.Left },
			[nameof(PayrollItemOverviewModel.Prorate)] = new() { DisplayName = "Prorate", Alignment = CellAlignment.Center },
			[nameof(PayrollItemOverviewModel.PaidDays)] = new() { DisplayName = "Paid Days", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.NetPay)] = new() { DisplayName = "Net Pay", Alignment = CellAlignment.Right, IncludeInTotal = false, Format = "#,##0.00" }
		};

		List<string> columnOrder =
		[
			nameof(PayrollItemOverviewModel.Id),
			nameof(PayrollItemOverviewModel.TransactionNo),
			nameof(PayrollItemOverviewModel.EmployeeCode),
			nameof(PayrollItemOverviewModel.EmployeeName),
			"Period",
			nameof(PayrollItemOverviewModel.SalaryComponentCode),
			nameof(PayrollItemOverviewModel.SalaryComponentName),
			nameof(PayrollItemOverviewModel.SalaryComponentType),
			nameof(PayrollItemOverviewModel.Amount),
			nameof(PayrollItemOverviewModel.Formula),
			nameof(PayrollItemOverviewModel.Prorate),
			nameof(PayrollItemOverviewModel.PaidDays),
			nameof(PayrollItemOverviewModel.NetPay)
		];

		var fileName = $"Payroll_Items_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"PAYROLL COMPONENTS",
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
				"PAYROLL COMPONENTS",
				"Payroll Component Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}

	private static string Period(int month, int year) =>
		month is < 1 or > 12
			? string.Empty
			: $"{CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month)} {year}";
}
