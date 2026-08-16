using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Stock;

namespace PrimeBakes.Library.Inventory.Stock.Exports;

public static class RawMaterialStockReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(RawMaterialStockReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportSummaryReport(
		IEnumerable<RawMaterialStockSummaryModel> stockData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportSummaryReport)),
			new RawMaterialStockSummaryReportRequest(stockData, exportType, dateRangeStart, dateRangeEnd, showAllColumns));

	public static async Task<(MemoryStream stream, string fileName)> ExportDetailsReport(
		IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportDetailsReport)),
			new RawMaterialStockDetailsReportRequest(stockDetailsData, exportType, dateRangeStart, dateRangeEnd));
}
