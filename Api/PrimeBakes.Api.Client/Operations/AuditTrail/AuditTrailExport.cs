using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Library.Operations.AuditTrail;

public static class AuditTrailExport
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AuditTrailExport));

	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<AuditTrailModel> data,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false) =>
		await ApiClient.PostForFile(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ExportReport)),
			new AuditTrailReportRequest(data, exportType, dateRangeStart, dateRangeEnd, showAllColumns));
}
