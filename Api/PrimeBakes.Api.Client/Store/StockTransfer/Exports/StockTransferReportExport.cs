using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Library.Store.StockTransfer.Exports;

public static class StockTransferReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(StockTransferReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<StockTransferOverviewModel> data,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new StockTransferReportRequest(data, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, company, fromLocation, toLocation));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<StockTransferItemOverviewModel> stockTransferItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new StockTransferItemReportRequest(stockTransferItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, company, fromLocation, toLocation));
}
