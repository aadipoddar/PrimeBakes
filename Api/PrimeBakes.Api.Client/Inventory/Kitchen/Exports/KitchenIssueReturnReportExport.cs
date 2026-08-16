using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenIssueReturnReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenIssueReturnOverviewModel> kitchenIssueReturnData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		KitchenModel kitchen = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new KitchenIssueReturnReportRequest(kitchenIssueReturnData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, kitchen, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<KitchenIssueReturnItemOverviewModel> kitchenIssueReturnItemData,
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
			new KitchenIssueReturnItemReportRequest(kitchenIssueReturnItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, rawMaterial, rawMaterialCategory, kitchen, company));
}
