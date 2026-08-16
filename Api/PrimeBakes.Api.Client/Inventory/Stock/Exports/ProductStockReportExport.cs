using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Library.Inventory.Stock.Exports;

public static class ProductStockReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(ProductStockReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportSummaryReport(
		IEnumerable<ProductStockSummaryModel> stockData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportSummaryReport)),
			new ProductStockSummaryReportRequest(stockData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, location));

	public static async Task<(MemoryStream stream, string fileName)> ExportDetailsReport(
		IEnumerable<ProductStockDetailsModel> stockDetailsData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportDetailsReport)),
			new ProductStockDetailsReportRequest(stockDetailsData, exportType, dateRangeStart, dateRangeEnd, location));
}
