using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenProductionReturnReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReturnReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenProductionReturnOverviewModel> kitchenProductionReturnData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new KitchenProductionReturnReportRequest(kitchenProductionReturnData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, kitchen, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<KitchenProductionReturnItemOverviewModel> kitchenProductionReturnItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new KitchenProductionReturnItemReportRequest(kitchenProductionReturnItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, kitchen, company));
}
