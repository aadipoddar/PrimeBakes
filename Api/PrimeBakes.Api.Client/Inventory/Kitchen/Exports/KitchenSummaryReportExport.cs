using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenSummaryReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenSummaryReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenSummaryModel> kitchenSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new KitchenSummaryReportRequest(kitchenSummaryData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));
}
