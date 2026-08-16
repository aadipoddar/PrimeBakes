using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Restaurant.Bill.Exports;

public static class BillReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BillReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<BillOverviewModel> billData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new BillReportRequest(billData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, company, location));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<BillItemOverviewModel> billItemData,
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
			new BillItemReportRequest(billItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, company, location));
}
