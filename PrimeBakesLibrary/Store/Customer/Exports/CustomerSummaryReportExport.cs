using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Store.Customer.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Store.Customer.Exports;

public static class CustomerSummaryReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<CustomerSummaryModel> customerSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(CustomerSummaryModel.Name)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.Number)] = new() { DisplayName = "Number", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.TotalTransactions)] = new() { DisplayName = "Transactions", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.SaleAmount)] = new() { DisplayName = "Sales", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.BillAmount)] = new() { DisplayName = "Bills", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.ReturnAmount)] = new() { DisplayName = "Returns", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.NetBusiness)] = new() { DisplayName = "Net Business", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(CustomerSummaryModel.LastPurchase)] = new() { DisplayName = "Last Visit", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.SaleCount)] = new() { DisplayName = "Sale Count", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.BillCount)] = new() { DisplayName = "Bill Count", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.ReturnCount)] = new() { DisplayName = "Return Count", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.TotalQuantity)] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.AverageOrderValue)] = new() { DisplayName = "Avg Order Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.ReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.ContributionPercent)] = new() { DisplayName = "Contribution %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.FirstPurchase)] = new() { DisplayName = "First Visit", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.DaysSinceLastVisit)] = new() { DisplayName = "Days Since Visit", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(CustomerSummaryModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(CustomerSummaryModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder = showAllColumns
			?
			[
				nameof(CustomerSummaryModel.Name),
				nameof(CustomerSummaryModel.Number),
				nameof(CustomerSummaryModel.TotalTransactions),
				nameof(CustomerSummaryModel.SaleCount),
				nameof(CustomerSummaryModel.BillCount),
				nameof(CustomerSummaryModel.ReturnCount),
				nameof(CustomerSummaryModel.SaleAmount),
				nameof(CustomerSummaryModel.BillAmount),
				nameof(CustomerSummaryModel.ReturnAmount),
				nameof(CustomerSummaryModel.NetBusiness),
				nameof(CustomerSummaryModel.TotalQuantity),
				nameof(CustomerSummaryModel.AverageOrderValue),
				nameof(CustomerSummaryModel.ReturnPercent),
				nameof(CustomerSummaryModel.ContributionPercent),
				nameof(CustomerSummaryModel.FirstPurchase),
				nameof(CustomerSummaryModel.LastPurchase),
				nameof(CustomerSummaryModel.DaysSinceLastVisit),
				nameof(CustomerSummaryModel.Cash),
				nameof(CustomerSummaryModel.Card),
				nameof(CustomerSummaryModel.UPI),
				nameof(CustomerSummaryModel.Credit)
			]
			:
			[
				nameof(CustomerSummaryModel.Name),
				nameof(CustomerSummaryModel.Number),
				nameof(CustomerSummaryModel.TotalTransactions),
				nameof(CustomerSummaryModel.SaleAmount),
				nameof(CustomerSummaryModel.BillAmount),
				nameof(CustomerSummaryModel.ReturnAmount),
				nameof(CustomerSummaryModel.NetBusiness),
				nameof(CustomerSummaryModel.LastPurchase)
			];

		string fileName = "CUSTOMER_SUMMARY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Company"] = company?.Name,
			["Location"] = location?.Name
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				customerSummaryData,
				"CUSTOMER SUMMARY REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true,
				metadata);

			return (stream, fileName + ".pdf");
		}

		var excelStream = await ExcelReportExportUtil.ExportToExcel(
			customerSummaryData,
			"CUSTOMER SUMMARY REPORT",
			"Customer Summary",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
