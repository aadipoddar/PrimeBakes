using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenProductionReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenProductionReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new KitchenProductionReportRequest(kitchenProductionData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, kitchen, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
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
			new KitchenProductionItemReportRequest(kitchenProductionItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, product, productCategory, kitchen, company));
}
