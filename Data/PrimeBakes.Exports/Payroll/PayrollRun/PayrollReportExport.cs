using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Models.Payroll.PayrollRun;

namespace PrimeBakes.Exports.Payroll.PayrollRun;

public static class PayrollReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<PayrollOverviewModel> payrollData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		EmployeeModel employee = null,
		DepartmentModel department = null,
		DesignationModel designation = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PayrollOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.EmployeeName)] = new() { DisplayName = "Employee", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.DepartmentName)] = new() { DisplayName = "Department", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.DesignationName)] = new() { DisplayName = "Designation", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.TransactionDateTime)] = new() { DisplayName = "Period", Format = "MMM yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.DaysInMonth)] = new() { DisplayName = "Days", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.PaidDays)] = new() { DisplayName = "Paid Days", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PayrollOverviewModel.GrossEarnings)] = new() { DisplayName = "Gross", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PayrollOverviewModel.TotalDeductions)] = new() { DisplayName = "Deductions", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PayrollOverviewModel.NetPay)] = new() { DisplayName = "Net Pay", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PayrollOverviewModel.EmployerContribution)] = new() { DisplayName = "Employer Contribution", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PayrollOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(PayrollOverviewModel.DepartmentName),
				nameof(PayrollOverviewModel.PaidDays),
				nameof(PayrollOverviewModel.GrossEarnings),
				nameof(PayrollOverviewModel.TotalDeductions),
				nameof(PayrollOverviewModel.NetPay),
				nameof(PayrollOverviewModel.EmployerContribution)
			];

			if (department is not null)
				columnOrder.Remove(nameof(PayrollOverviewModel.DepartmentName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PayrollOverviewModel.TransactionNo),
				nameof(PayrollOverviewModel.EmployeeCode),
				nameof(PayrollOverviewModel.EmployeeName),
				nameof(PayrollOverviewModel.DepartmentName),
				nameof(PayrollOverviewModel.DesignationName),
				nameof(PayrollOverviewModel.TransactionDateTime),
				nameof(PayrollOverviewModel.FinancialYear),
				nameof(PayrollOverviewModel.DaysInMonth),
				nameof(PayrollOverviewModel.PaidDays),
				nameof(PayrollOverviewModel.GrossEarnings),
				nameof(PayrollOverviewModel.TotalDeductions),
				nameof(PayrollOverviewModel.NetPay),
				nameof(PayrollOverviewModel.EmployerContribution),
				nameof(PayrollOverviewModel.Remarks),
				nameof(PayrollOverviewModel.CreatedByName),
				nameof(PayrollOverviewModel.CreatedAt),
				nameof(PayrollOverviewModel.CreatedFormFactor),
				nameof(PayrollOverviewModel.CreatedPlatform),
				nameof(PayrollOverviewModel.CreatedLatitude),
				nameof(PayrollOverviewModel.CreatedLongitude),
				nameof(PayrollOverviewModel.LastModifiedByUserName),
				nameof(PayrollOverviewModel.LastModifiedAt),
				nameof(PayrollOverviewModel.LastModifiedFormFactor),
				nameof(PayrollOverviewModel.LastModifiedPlatform),
				nameof(PayrollOverviewModel.LastModifiedLatitude),
				nameof(PayrollOverviewModel.LastModifiedLongitude),
				nameof(PayrollOverviewModel.CreatedUserOffset),
				nameof(PayrollOverviewModel.LastModifiedUserOffset),
				nameof(PayrollOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PayrollOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(PayrollOverviewModel.TransactionNo),
				nameof(PayrollOverviewModel.EmployeeCode),
				nameof(PayrollOverviewModel.EmployeeName),
				nameof(PayrollOverviewModel.DepartmentName),
				nameof(PayrollOverviewModel.TransactionDateTime),
				nameof(PayrollOverviewModel.PaidDays),
				nameof(PayrollOverviewModel.GrossEarnings),
				nameof(PayrollOverviewModel.TotalDeductions),
				nameof(PayrollOverviewModel.NetPay),
				nameof(PayrollOverviewModel.Status)
			];

			if (employee is not null)
			{
				columnOrder.Remove(nameof(PayrollOverviewModel.EmployeeCode));
				columnOrder.Remove(nameof(PayrollOverviewModel.EmployeeName));
			}

			if (department is not null)
				columnOrder.Remove(nameof(PayrollOverviewModel.DepartmentName));

			if (!showDeleted)
				columnOrder.Remove(nameof(PayrollOverviewModel.Status));
		}

		string fileName = "PAYROLL_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		Dictionary<string, string> filters = new()
		{
			["Employee"] = employee?.Name ?? null,
			["Department"] = department?.Name ?? null,
			["Designation"] = designation?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				payrollData,
				"PAYROLL REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				filters
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				payrollData,
				"PAYROLL REPORT",
				"Payroll Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				filters
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static (MemoryStream stream, string fileName) ExportItemReport(
		IEnumerable<PayrollItemOverviewModel> payrollItemData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		EmployeeModel employee = null,
		DepartmentModel department = null,
		SalaryComponentModel salaryComponent = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PayrollItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.EmployeeCode)] = new() { DisplayName = "Employee Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.EmployeeName)] = new() { DisplayName = "Employee", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Period", Format = "MMM yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.SalaryComponentCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.SalaryComponentName)] = new() { DisplayName = "Component", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.SalaryComponentType)] = new() { DisplayName = "Type", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.Amount)] = new() { DisplayName = "Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PayrollItemOverviewModel.Formula)] = new() { DisplayName = "Formula", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.Prorate)] = new() { DisplayName = "Prorate", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.PaidDays)] = new() { DisplayName = "Paid Days", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PayrollItemOverviewModel.NetPay)] = new() { DisplayName = "Net Pay", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(PayrollItemOverviewModel.SalaryComponentCode),
				nameof(PayrollItemOverviewModel.SalaryComponentName),
				nameof(PayrollItemOverviewModel.SalaryComponentType),
				nameof(PayrollItemOverviewModel.Amount)
			];

			if (salaryComponent is not null)
			{
				columnOrder.Remove(nameof(PayrollItemOverviewModel.SalaryComponentCode));
				columnOrder.Remove(nameof(PayrollItemOverviewModel.SalaryComponentName));
			}
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PayrollItemOverviewModel.TransactionNo),
				nameof(PayrollItemOverviewModel.EmployeeCode),
				nameof(PayrollItemOverviewModel.EmployeeName),
				nameof(PayrollItemOverviewModel.TransactionDateTime),
				nameof(PayrollItemOverviewModel.SalaryComponentCode),
				nameof(PayrollItemOverviewModel.SalaryComponentName),
				nameof(PayrollItemOverviewModel.SalaryComponentType),
				nameof(PayrollItemOverviewModel.Amount),
				nameof(PayrollItemOverviewModel.Formula),
				nameof(PayrollItemOverviewModel.Prorate),
				nameof(PayrollItemOverviewModel.PaidDays),
				nameof(PayrollItemOverviewModel.NetPay)
			];
		}

		else
		{
			columnOrder =
			[
				nameof(PayrollItemOverviewModel.EmployeeCode),
				nameof(PayrollItemOverviewModel.EmployeeName),
				nameof(PayrollItemOverviewModel.TransactionDateTime),
				nameof(PayrollItemOverviewModel.SalaryComponentCode),
				nameof(PayrollItemOverviewModel.SalaryComponentName),
				nameof(PayrollItemOverviewModel.Amount)
			];

			if (employee is not null)
			{
				columnOrder.Remove(nameof(PayrollItemOverviewModel.EmployeeCode));
				columnOrder.Remove(nameof(PayrollItemOverviewModel.EmployeeName));
			}
		}

		string fileName = "PAYROLL_COMPONENT_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		Dictionary<string, string> filters = new()
		{
			["Employee"] = employee?.Name ?? null,
			["Department"] = department?.Name ?? null,
			["Component"] = salaryComponent?.Name ?? null
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				payrollItemData,
				"PAYROLL COMPONENT REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns,
				filters
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				payrollItemData,
				"PAYROLL COMPONENT REPORT",
				"Payroll Components",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				filters
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
