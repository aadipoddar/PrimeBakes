using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

public static class PurchaseReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PurchaseReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseOverviewModel> purchaseData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new PurchaseReportRequest(purchaseData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, party, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		CompanyModel company = null,
		LedgerModel party = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportItemReport)),
			new PurchaseItemReportRequest(purchaseItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, rawMaterial, rawMaterialCategory, company, party));
}
