using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenIssueReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenIssueOverviewModel> kitchenIssueData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new KitchenIssueReportRequest(kitchenIssueData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, kitchen, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new KitchenIssueItemReportRequest(kitchenIssueItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, rawMaterial, rawMaterialCategory, kitchen, company));
}
