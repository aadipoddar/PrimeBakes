using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Library.Store.Sale.Exports;

public static class OutletSummaryReportExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(OutletSummaryReportExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<OutletSummaryModel> outletSummaryData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false,
		CompanyModel company = null) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new OutletSummaryReportRequest(outletSummaryData, exportType, dateRangeStart, dateRangeEnd, showAllColumns, company));
}
