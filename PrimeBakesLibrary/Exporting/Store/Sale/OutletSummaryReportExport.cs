using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Store.Sale;

namespace PrimeBakesLibrary.Exporting.Store.Sale;

public static class OutletSummaryReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<OutletSummaryModel> outletSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OutletSummaryModel.LocationName)] = new()
			{
				DisplayName = "Outlet",
				Alignment = CellAlignment.Left,
				IncludeInTotal = false
			},
			[nameof(OutletSummaryModel.Purchase)] = new()
			{
				DisplayName = "Purchase",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},
			[nameof(OutletSummaryModel.PurchaseReturn)] = new()
			{
				DisplayName = "Purchase Return",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},
			[nameof(OutletSummaryModel.KitchenIssue)] = new()
			{
				DisplayName = "Kitchen Issue",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},
			[nameof(OutletSummaryModel.KitchenProduction)] = new()
			{
				DisplayName = "Kitchen Production",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},
			[nameof(OutletSummaryModel.Sale)] = new()
			{
				DisplayName = "Sale",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},
			[nameof(OutletSummaryModel.SaleReturn)] = new()
			{
				DisplayName = "Sale Return",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			}
		};

		List<string> columnOrder =
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
