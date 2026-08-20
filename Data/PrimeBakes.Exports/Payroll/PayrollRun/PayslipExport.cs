using System.Globalization;

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Masters;
using PrimeBakes.Models.Payroll.PayrollRun;

namespace PrimeBakes.Exports.Payroll.PayrollRun;

public static class PayslipExport
{
	public static (MemoryStream stream, string fileName) ExportPayslip(PayslipBundle bundle, InvoiceExportType exportType)
	{
		var (payroll, components, company, employee, currentDateTime) = bundle;

		var infoType = SalaryComponentTypes.Info.ToString();

		var lineItems = components
			.Where(component => component.SalaryComponentType != infoType && component.Amount != 0)
			.OrderBy(component => component.Sequence)
			.Select(component => new
			{
				component.SalaryComponentCode,
				component.SalaryComponentName,
				component.SalaryComponentType,
				component.Amount
			})
			.ToList();

		var employeeAsLedger = new LedgerModel
		{
			Name = $"{employee.Code} - {employee.Name}"
		};

		var period = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth)} {payroll.PayrollYear}";

		var invoiceData = new InvoiceData
		{
			CurrentDateTime = currentDateTime,
			Company = company,
			BillTo = employeeAsLedger,
			InvoiceType = $"PAYSLIP - {period.ToUpper()}",
			Outlet = payroll.DepartmentName ?? string.Empty,
			TransactionNo = payroll.TransactionNo,
			TransactionDateTime = payroll.TransactionDateTime,
			TotalAmount = payroll.NetPay,
			Remarks = payroll.Remarks ?? string.Empty,
			Status = payroll.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Days In Month"] = payroll.DaysInMonth.ToString("N2"),
			["Paid Days"] = payroll.PaidDays.ToString("N2"),
			["Gross Earnings"] = payroll.GrossEarnings.FormatIndianCurrency(),
			["Total Deductions"] = payroll.TotalDeductions.FormatIndianCurrency(),
			["Net Pay"] = payroll.NetPay.FormatIndianCurrency(),
			["Employer Contribution"] = payroll.EmployerContribution.FormatIndianCurrency(),
			["Cost To Company"] = (payroll.GrossEarnings + payroll.EmployerContribution).FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(PayrollItemOverviewModel.SalaryComponentCode), "Code", exportType, CellAlignment.Left, 70, 14),
			new(nameof(PayrollItemOverviewModel.SalaryComponentName), "Component", exportType, CellAlignment.Left, 0, 35),
			new(nameof(PayrollItemOverviewModel.SalaryComponentType), "Type", exportType, CellAlignment.Left, 120, 22),
			new(nameof(PayrollItemOverviewModel.Amount), "Amount", exportType, CellAlignment.Right, 80, 16, "#,##0.00")
		};

		string fileName = $"PAYSLIP_{employee.Code}_{payroll.PayrollYear}{payroll.PayrollMonth:00}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = PDFInvoiceExportUtil.ExportInvoiceToPdf(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelInvoiceExportUtil.ExportInvoiceToExcel(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
