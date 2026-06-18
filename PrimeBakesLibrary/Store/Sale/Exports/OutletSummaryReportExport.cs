using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Store.Sale.Exports;

public static class OutletSummaryReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<OutletSummaryModel> outletSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OutletSummaryModel.LocationName)] = new() { DisplayName = "Outlet", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OutletSummaryModel.Purchase)] = new() { DisplayName = "Purchase", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.PurchaseReturn)] = new() { DisplayName = "Purchase Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.NetPurchase)] = new() { DisplayName = "Net Purchase", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(OutletSummaryModel.KitchenIssue)] = new() { DisplayName = "Kitchen Issue", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.KitchenProduction)] = new() { DisplayName = "Kitchen Production", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.Sale)] = new() { DisplayName = "Sale", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.SaleReturn)] = new() { DisplayName = "Sale Return", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.NetSale)] = new() { DisplayName = "Net Sale", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(OutletSummaryModel.SaleReturnPercent)] = new() { DisplayName = "Return %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OutletSummaryModel.GrossProfit)] = new() { DisplayName = "Gross Profit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(OutletSummaryModel.MarginPercent)] = new() { DisplayName = "Margin %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(OutletSummaryModel.ContributionPercent)] = new() { DisplayName = "Contribution %", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OutletSummaryModel.TransactionCount)] = new() { DisplayName = "Transactions", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.UnitsSold)] = new() { DisplayName = "Units Sold", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.AverageSaleValue)] = new() { DisplayName = "Avg Sale Value", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OutletSummaryModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OutletSummaryModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true }
		};

		List<string> columnOrder = showAllColumns
			?
			[
				nameof(OutletSummaryModel.LocationName),
				nameof(OutletSummaryModel.Purchase),
				nameof(OutletSummaryModel.PurchaseReturn),
				nameof(OutletSummaryModel.NetPurchase),
				nameof(OutletSummaryModel.KitchenIssue),
				nameof(OutletSummaryModel.KitchenProduction),
				nameof(OutletSummaryModel.Sale),
				nameof(OutletSummaryModel.SaleReturn),
				nameof(OutletSummaryModel.NetSale),
				nameof(OutletSummaryModel.SaleReturnPercent),
				nameof(OutletSummaryModel.GrossProfit),
				nameof(OutletSummaryModel.MarginPercent),
				nameof(OutletSummaryModel.ContributionPercent),
				nameof(OutletSummaryModel.TransactionCount),
				nameof(OutletSummaryModel.UnitsSold),
				nameof(OutletSummaryModel.AverageSaleValue),
				nameof(OutletSummaryModel.Cash),
				nameof(OutletSummaryModel.Card),
				nameof(OutletSummaryModel.UPI),
				nameof(OutletSummaryModel.Credit)
			]
			:
			[
				nameof(OutletSummaryModel.LocationName),
				nameof(OutletSummaryModel.Purchase),
				nameof(OutletSummaryModel.PurchaseReturn),
				nameof(OutletSummaryModel.KitchenIssue),
				nameof(OutletSummaryModel.KitchenProduction),
				nameof(OutletSummaryModel.Sale),
				nameof(OutletSummaryModel.SaleReturn)
			];

		string fileName = "OUTLET_SUMMARY_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		var metadata = new Dictionary<string, string>
		{
			["Company"] = company?.Name
		};

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				outletSummaryData,
				"OUTLET SUMMARY REPORT",
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
			outletSummaryData,
			"OUTLET SUMMARY REPORT",
			"Outlet Summary",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			metadata);

		return (excelStream, fileName + ".xlsx");
	}
}
