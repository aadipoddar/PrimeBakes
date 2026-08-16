using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

public static class PurchaseReturnReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(PurchaseReturnReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseReturnOverviewModel> purchaseData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new PurchaseReturnReportRequest(purchaseData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, party, company));

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<PurchaseReturnItemOverviewModel> purchaseItemData,
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
			new PurchaseReturnItemReportRequest(purchaseItemData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, showDeleted, showSummary, rawMaterial, rawMaterialCategory, company, party));
}
