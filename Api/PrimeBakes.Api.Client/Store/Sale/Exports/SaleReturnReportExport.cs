using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Library.Store.Sale.Exports;

public static class SaleReturnReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SaleReturnReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<SaleReturnOverviewModel> saleReturnData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null,
		LocationModel location = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new SaleReturnReportRequest(saleReturnData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, party, company, location));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<SaleReturnItemOverviewModel> saleReturnItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel location = null,
		LedgerModel party = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new SaleReturnItemReportRequest(saleReturnItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, company, location, party));
}
