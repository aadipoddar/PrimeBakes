using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Store.Order.Exports;

public static class OrderReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(OrderReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<OrderOverviewModel> orderData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new OrderReportRequest(orderData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, company, location));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<OrderItemOverviewModel> orderItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new OrderItemReportRequest(orderItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, company, location));
}
